using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Flagsmith
{
    public class PollingManager : IPollingManager
    {
        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();
        Func<Task> _CallBack;
        int _Interval;
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
        /// Start calling callback continously after provided interval
        /// </summary>
        /// <returns>Task</returns>
        public async Task StartPoll()
        {
            _CancellationTokenSource.Token.ThrowIfCancellationRequested();
            while (true)
            {
                await _CallBack.Invoke();
                await Task.Delay(_Interval, _CancellationTokenSource.Token);
                if (_CancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }
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