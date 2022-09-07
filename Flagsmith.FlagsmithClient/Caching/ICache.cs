using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith.Caching
{
    public interface ICache
    {
        T GetObject<T>(string key, Func<ICacheItemPolicy, T> createObject);
        Task<T> GetObjectAsync<T>(string key, Func<ICacheItemPolicy, Task<T>> createObject);

        bool TryGetObject<T>(string key, out T obj);
        T TryGetObject<T>(string key);
        Task<T> TryGetObjectAsync<T>(string key);

        void Clear();
        Task ClearAsync();

        void Remove(string key);
        Task RemoveAsync(string key);

        void RemoveAll(Func<string, bool> func);
        Task RemoveAllAsync(Func<string, bool> func);

        IReadOnlyCollection<ICacheItem> GetItems();
    }
}
