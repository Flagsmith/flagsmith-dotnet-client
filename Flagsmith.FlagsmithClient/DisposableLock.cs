using System;
using System.Threading;

namespace Flagsmith
{
    internal class DisposableLock
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public IDisposable AcquireLock()
        {
            var theLock = new TheLock(_semaphore);
            theLock.Wait();

            return theLock;
        }

        internal class TheLock : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public TheLock(SemaphoreSlim semaphore)
            {
                this._semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore?.Release();
            }

            public void Wait()
            {
                _semaphore?.Wait();
            }
        }
    }
}
