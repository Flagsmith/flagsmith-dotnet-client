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
        private readonly CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();
        readonly Func<Task> _CallBack;
        readonly int _Interval;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callBack">Awaitable function that will be polled.</param>
        /// <param name="intervalSeconds">Total delay in seconds between continous exection of callback.</param>
        public PollingManager(Func<Task> callBack, int intervalSeconds = 10)
        {
            _CallBack = callBack;
            _Interval = intervalSeconds * 1000; //convert to milliseconds
        }
        /// <summary>
        /// Start calling callback continuously after provided interval
        /// </summary>
        /// <returns>Task</returns>
        public async Task StartPoll()
        {
            // Force a first call of the callback at least once and synchronously if it is awaited.
            await _CallBack.Invoke();
            _CancellationTokenSource.Token.ThrowIfCancellationRequested();
            _timer = new Timer(async (object state) =>
            {
                if (_CancellationTokenSource.Token.IsCancellationRequested)
                {
                    _timer.Dispose();
                    return;
                }
                await _CallBack.Invoke();
            }, null, _Interval, _Interval);
        }
        /// <summary>
        /// Stop continously exectuing callback
        /// </summary>
        public void StopPoll()
        {
            _CancellationTokenSource.Cancel();
        }
    }
}