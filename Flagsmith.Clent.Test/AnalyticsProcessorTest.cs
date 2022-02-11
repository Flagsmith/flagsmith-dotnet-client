using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Flagsmith.DotnetClient.Test
{
    internal class AnalyticsProcessorTest : AnalyticsProcessor
    {
        Dictionary<string, int> _totalFucntionCalls;
        public bool IsFlushEarlyReturn { get; private set; } = false;
        public AnalyticsProcessorTest(HttpClient httpClient, string environmentKey, string baseApiUrl, int timeOut = 3)
            : base(httpClient, environmentKey, baseApiUrl, timeOut)
        {
            _totalFucntionCalls = new Dictionary<string, int>();
        }

        public override async Task Flush()
        {
            IsFlushEarlyReturn = false;
            if (!AnalyticsData.Any())
            {
                IsFlushEarlyReturn = true;
                _LogFunctionCall(nameof(Flush));
                return;
            }

            await Task.Delay(0);
            this.AnalyticsData.Clear();
            _LogFunctionCall(nameof(Flush));
        }
        /// <summary>
        /// Returns tracked feature counts that are not posted on the server yet.
        /// </summary>
        /// <param name="featureId"></param>
        /// <returns></returns>
        public int this[int featureId] => AnalyticsData[featureId];
        public int this[string functionName] => _totalFucntionCalls.GetValueOrDefault(functionName);
        public bool HasTrackingItemsInCache() => AnalyticsData.Any();
        public override string ToString() => JsonConvert.SerializeObject(AnalyticsData);
        private void _LogFunctionCall(string functionName)
        {
            _totalFucntionCalls[functionName] = _totalFucntionCalls.TryGetValue(functionName, out int i) ? i + 1 : 1;
        }
    }
}
