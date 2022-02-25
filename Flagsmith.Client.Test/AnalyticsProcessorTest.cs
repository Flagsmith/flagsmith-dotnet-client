using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Flagsmith.FlagsmithClientTest
{
    internal class AnalyticsProcessorTest : AnalyticsProcessor
    {
        /// <summary>
        /// This class provides some extra functionality to help particularly in unit testing.
        /// </summary>
        public AnalyticsProcessorTest(HttpClient httpClient, string environmentKey, string baseApiUrl, int timeOut = 3)
            : base(httpClient, environmentKey, baseApiUrl, timeOut: timeOut)
        {
        }
        /// <summary>
        /// Returns tracked feature counts that are not posted on the server yet.
        /// </summary>
        /// <param name="featureId"></param>
        /// <returns></returns>
        public int this[int featureId] => AnalyticsData[featureId];
        /// <summary>
        /// Check if there are any items in analytics cache.
        /// </summary>
        /// <returns></returns>
        public bool HasTrackingItemsInCache() => AnalyticsData.Any();
        public override string ToString() => JsonConvert.SerializeObject(AnalyticsData);
    }
}
