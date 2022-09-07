using Flagsmith.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith.Caching.Impl
{
    public class MemoryCache : ICache, IDisposable, IAsyncDisposable
    {
        private class Holder : ICacheItemPolicy, IDisposable, IAsyncDisposable
        {
            private object _value;

            public Guid UniqueKey { get; }
            public DateTimeOffset LastAccessed { get; private set; }
            public object Value { get { return _value; } set { _value = value; } }
            public DateTimeOffset Created { get; }
            public DateTimeOffset AbsoluteExpiration { get; set; }
            public TimeSpan SlidingExpiration { get; set; }

            public void Accessed()
            {
                LastAccessed = DateTimeOffset.Now;
            }

            public void Dispose()
            {
                var value = Interlocked.Exchange(ref _value, null);
                if (value is IDisposable disposable)
                    disposable.Dispose();

                GC.SuppressFinalize(this);
            }

            public async ValueTask DisposeAsync()
            {
                var value = Interlocked.Exchange(ref _value, null);
                if (value is IAsyncDisposable disposable)
                    await disposable.DisposeAsync();

                GC.SuppressFinalize(this);
            }

            public Holder()
            {
                UniqueKey = Guid.NewGuid();
                Created = DateTimeOffset.Now;
                AbsoluteExpiration = DateTimeOffset.MaxValue;
                SlidingExpiration = TimeSpan.MaxValue;
            }
        }

        private class CacheItem : ICacheItem
        {
            public string Key { get; }
            public Guid UniqueKey { get; }
            public Type ValueType { get; }
            public DateTimeOffset Created { get; }
            public DateTimeOffset LastAccessed { get; }
            public DateTimeOffset AbsoluteExpiration { get; }
            public TimeSpan SlidingExpiration { get; }

            public CacheItem(string key, Holder holder)
            {
                Key = key;
                UniqueKey = holder.UniqueKey;
                ValueType = holder.Value?.GetType();
                Created = holder.Created;
                LastAccessed = holder.LastAccessed;
                AbsoluteExpiration = holder.AbsoluteExpiration;
                SlidingExpiration = holder.SlidingExpiration;
            }
        }

        private static readonly TimeSpan CleanPeriod = TimeSpan.FromMinutes(10);

        private readonly Dictionary<string, Holder> _objects = new Dictionary<string, Holder>();
        private readonly Pool<SemaphoreSlimSync> _semaphores = new Pool<SemaphoreSlimSync>();
        private readonly object _sync = new object();
        private DateTimeOffset _lastClean = DateTimeOffset.Now;

        public T GetObject<T>(string key, Func<ICacheItemPolicy, T> createObject)
        {
            var result = GetHolder(key);
            if (result.Item2 == null)
                return (T)result.Item1.Value;

            try
            {
                using (result.Item2.Value.Wait())
                {
                    var concurrent = CheckConcurrent(key, result.Item1);
                    if (concurrent != null)
                        return (T)concurrent.Value;

                    result.Item1.Value = createObject(result.Item1);
                    return FinishHolder<T>(key, result.Item1);
                }
            }
            catch
            {
                result.Item1.Dispose();
                throw;
            }
            finally
            {
                result.Item2.Dispose();
            }
        }

        public async Task<T> GetObjectAsync<T>(string key, Func<ICacheItemPolicy, Task<T>> createObject)
        {
            var result = await GetHolderAsync(key);
            if (result.Item2 == null)
                return (T)result.Item1.Value;

            try
            {
                using (await result.Item2.Value.WaitAsync())
                {
                    var concurrent = await CheckConcurrentAsync(key, result.Item1);
                    if (concurrent != null)
                        return (T)concurrent.Value;

                    result.Item1.Value = await createObject(result.Item1);
                    return FinishHolder<T>(key, result.Item1);
                }
            }
            catch
            {
                await result.Item1.DisposeAsync();
                throw;
            }
            finally
            {
                result.Item2.Dispose();
            }
        }

        public bool TryGetObject<T>(string key, out T obj)
        {
            obj = TryGetObject<T>(key);
            return !EqualityComparer<T>.Default.Equals(obj, default);
        }

        public T TryGetObject<T>(string key)
        {
            Holder holder;
            lock (_sync)
            {
                if (_objects.TryGetValue(key, out holder) && holder.IsStillValid(holder.LastAccessed))
                {
                    holder.Accessed();
                    return (T)holder.Value;
                }
                if (holder != null)
                    _objects.Remove(key);
            }

            if (holder != null)
                holder.Dispose();

            return default;
        }

        public async Task<T> TryGetObjectAsync<T>(string key)
        {
            Holder holder;
            lock (_sync)
            {
                if (_objects.TryGetValue(key, out holder) && holder.IsStillValid(holder.LastAccessed))
                {
                    holder.Accessed();
                    return (T)holder.Value;
                }
                if (holder != null)
                    _objects.Remove(key);
            }

            if (holder != null)
                await holder.DisposeAsync();

            return default;
        }

        public void Clear()
        {
            IEnumerable<Holder> holders;
            lock (_sync)
            {
                holders = _objects.Select(x => x.Value).ToArray();
                _objects.Clear();
            }

            holders.ForEach(x => x.Dispose());

            GC.Collect();
        }

        public async Task ClearAsync()
        {
            IEnumerable<Holder> holders;
            lock (_sync)
            {
                holders = _objects.Select(x => x.Value).ToArray();
                _objects.Clear();
            }

            await holders.ForEachAsync(async (x) => await x.DisposeAsync());

            GC.Collect();
        }

        private bool FindHolder(ref string key, out Holder holder)
        {
            if (string.IsNullOrEmpty(key))
            {
                holder = null;
                return false;
            }
            lock (_sync)
            {
                if (!_objects.TryGetValue(key, out holder))
                {
                    if (!Guid.TryParse(key, out var uniqueKey))
                        return false;

                    var pair = _objects.FirstOrDefault(x => x.Value.UniqueKey == uniqueKey);
                    holder = pair.Value;
                    key = pair.Key;

                    if (holder == null)
                        return false;
                }
                _objects.Remove(key);
            }
            return true;
        }

        public void Remove(string key)
        {
            if (FindHolder(ref key, out var holder))
                holder.Dispose();
        }

        public async Task RemoveAsync(string key)
        {
            if (FindHolder(ref key, out var holder))
                await holder.DisposeAsync();
        }

        public void RemoveAll(Func<string, bool> func)
        {
            if (func == null)
            {
                Clear();
                return;
            }

            List<KeyValuePair<string, Holder>> toRemove;
            lock (_sync)
            {
                toRemove = _objects.Where(x => func(x.Key) || func(x.Value.UniqueKey.ToString())).ToList();
                toRemove.ForEach(x => _objects.Remove(x.Key));
            }

            toRemove.ForEach(x => x.Value.Dispose());
        }

        public async Task RemoveAllAsync(Func<string, bool> func)
        {
            if (func == null)
            {
                await ClearAsync();
                return;
            }

            List<KeyValuePair<string, Holder>> toRemove;
            lock (_sync)
            {
                toRemove = _objects.Where(x => func(x.Key) || func(x.Value.UniqueKey.ToString())).ToList();
                toRemove.ForEach(x => _objects.Remove(x.Key));
            }

            await toRemove.ForEachAsync(async x => await x.Value.DisposeAsync());
        }

        public IReadOnlyCollection<ICacheItem> GetItems()
        {
            lock (_sync)
                return _objects.Select(x => new CacheItem(x.Key, x.Value)).ToList();
        }

        private IReadOnlyCollection<KeyValuePair<string, Holder>> GetHolderAndExpired(string key, out Holder holder, out IPooled<SemaphoreSlimSync> sync)
        {
            lock (_sync)
            {
                var expired = RemoveExpired();
                if (_objects.TryGetValue(key, out holder) && holder.IsStillValid(holder.LastAccessed))
                {
                    sync = null;
                    holder.Accessed();
                    return expired;
                }

                if (holder != null)
                {
                    _objects.Remove(key);

                    if (expired == null)
                        expired = new List<KeyValuePair<string, Holder>>();

                    expired.Add(new KeyValuePair<string, Holder>(key, holder));
                }

                holder = new Holder();
                sync = _semaphores.Get(key);
                return expired;
            }
        }

        private async Task<(Holder, IPooled<SemaphoreSlimSync>)> GetHolderAsync(string key)
        {
            var expired = GetHolderAndExpired(key, out var holder, out var sync);
            if (expired != null && expired.Count > 0)
                await expired.ForEachAsync(async x => await x.Value.DisposeAsync());
            return (holder, sync);
        }

        private (Holder, IPooled<SemaphoreSlimSync>) GetHolder(string key)
        {
            var expired = GetHolderAndExpired(key, out var holder, out var sync);
            if (expired != null && expired.Count > 0)
                expired.ForEach(x => x.Value.Dispose());
            return (holder, sync);
        }

        private async Task<Holder> CheckConcurrentAsync(string key, Holder holder)
        {
            Holder concurrent;
            lock (_sync)
            {
                if (!_objects.TryGetValue(key, out concurrent))
                    return null;
            }

            await holder.DisposeAsync();
            concurrent.Accessed();
            return concurrent;
        }

        private Holder CheckConcurrent(string key, Holder holder)
        {
            Holder concurrent;
            lock (_sync)
            {
                if (!_objects.TryGetValue(key, out concurrent))
                    return null;
            }

            holder.Dispose();
            concurrent.Accessed();
            return concurrent;
        }

        private T FinishHolder<T>(string key, Holder holder)
        {
            lock (_sync)
            {
                _objects[key] = holder;
                holder.Accessed();
                return (T)holder.Value;
            }
        }

        private List<KeyValuePair<string, Holder>> RemoveExpired()
        {
            var now = DateTimeOffset.Now;
            if (_lastClean + CleanPeriod < now)
                return null;

            var expired = _objects.Where(x => !x.Value.IsStillValid(x.Value.LastAccessed)).ToList();
            foreach (var pair in expired)
                _objects.Remove(pair.Key);

            _lastClean = now;
            return expired;
        }

        ~MemoryCache()
        {
            Clear();
        }

        public async ValueTask DisposeAsync()
        {
            await ClearAsync();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }
    }
}
