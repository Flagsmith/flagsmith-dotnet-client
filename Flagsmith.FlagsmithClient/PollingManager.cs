using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Flagsmith
{
    public class PollingManager : IPollingManager
    {
        private Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<Task> _callback;
        private readonly TimeSpan _interval;
        /// <param name="callback">Awaitable function that will be polled.</param>
        /// <param name="timespan">Polling interval.</param>
        public PollingManager(Func<Task> callback, TimeSpan timespan)
        {
            this._callback = callback;
            _interval = timespan;
        }
        /// <summary>
        /// Start calling callback continuously after provided interval
        /// </summary>
        /// <returns>Task</returns>
        public async Task StartPoll()
        {
            // Force a first call of the callback at least once and synchronously if it is awaited.
            await _callback.Invoke();
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            _timer = new Timer(state =>
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _timer.Dispose();
                    return;
                }
                _callback.Invoke().GetAwaiter().GetResult();
            }, null, _interval, _interval);
        }
        /// <summary>
        /// Stop continuously executing callback
        /// </summary>
        public void StopPoll()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}