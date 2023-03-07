using System.Threading.Tasks;

namespace Flagsmith
{
    public interface IPollingManager
    {
        /// <summary>
        /// Start calling callback continously after provided interval
        /// </summary>
        /// <returns>Task</returns>
        Task StartPoll();

        /// <summary>
        /// Stop continously exectuing callback
        /// </summary>
        void StopPoll();
    }
}