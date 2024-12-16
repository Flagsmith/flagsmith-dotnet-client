using System;
using System.Threading;

namespace Flagsmith
{
    /// <summary>
    /// This class is to ensure that Flush doesn't flush the same data multiple times.
    /// It functions basically like an async lock but with disposable semantics.
    /// Opportunity to improve it to use `WaitAsync` and `ReleaseAsync` in future.
    /// </summary>
    internal class DisposableLock
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        internal IDisposable AcquireLock()
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

            internal void Wait()
            {
                _semaphore?.Wait();
            }
        }
    }
}
