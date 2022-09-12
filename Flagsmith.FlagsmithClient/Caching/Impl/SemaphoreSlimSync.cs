using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith.Caching.Impl
{
    public class SemaphoreSlimSync
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public void Dispose()
        {
            _semaphore?.Dispose();
            _semaphore = null;
        }

        public IDisposable Wait(Action releaseAction = null)
        {
            _semaphore.Wait();
            return new SemaphoreSlimSection(_semaphore, releaseAction);
        }

        public async Task<IDisposable> WaitAsync(Action releaseAction = null)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            return new SemaphoreSlimSection(_semaphore, releaseAction);
        }

        private class SemaphoreSlimSection : IDisposable
        {
            private SemaphoreSlim _semaphore;
            private Action _releaseAction;

            public SemaphoreSlimSection(SemaphoreSlim semaphore, Action releaseAction = null)
            {
                _semaphore = semaphore;
                _releaseAction = releaseAction;
            }

            public void Dispose()
            {
                var temp = Interlocked.Exchange(ref _semaphore, null);
                if (temp != null)
                {
                    try
                    {
                        _releaseAction?.Invoke();
                    }
                    catch
                    {
                    }
                    _releaseAction = null;
                    temp.Release();
                }
            }
        }
    }
}
