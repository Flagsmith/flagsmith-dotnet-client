using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith
{
    public interface IAnalyticsProcessor
    {
        /// <summary>
        /// Post the features on the provided endpoint and clear the cached data.
        /// </summary>
        /// <returns></returns>
        Task Flush();

        /// <summary>
        /// Send analytics to server about feature usage.
        /// </summary>
        /// <param name="featureId"></param>
        /// <returns></returns>
        Task TrackFeature(string featureName);

        /// <summary>
        /// Get aggregated analytics data.
        /// </summary>
        /// <returns>Dictionary of feature name and usage count</returns>
        public Dictionary<string, int> GetAggregatedAnalytics();
    }
}