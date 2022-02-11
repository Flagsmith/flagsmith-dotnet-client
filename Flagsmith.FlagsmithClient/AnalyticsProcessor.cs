using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace Flagsmith
{
    public class AnalyticsProcessor
    {
        const int AnalyticsTimer = 10;
        readonly string _AnalyticsEndPoint;
        readonly string _EnvironmentKey;
        readonly int _TimeOut;
        DateTime _LastFlushed;
        protected Dictionary<int, int> AnalyticsData;
        HttpClient _HttpClient;

        public AnalyticsProcessor(HttpClient httpClient, string environmentKey, string baseApiUrl, int timeOut = 3)
        {
            _EnvironmentKey = environmentKey;
            _AnalyticsEndPoint = baseApiUrl + "analytics/flags/";
            _TimeOut = timeOut;
            _LastFlushed = DateTime.Now;
            AnalyticsData = new Dictionary<int, int>();
            _HttpClient = httpClient;
        }
        /// <summary>
        /// Post the features on the provided endpoint and clear the cached data.
        /// </summary>
        /// <returns></returns>
        public virtual async Task Flush()
        {
            if (AnalyticsData?.Any() == false)
                return;
            var request = new HttpRequestMessage(HttpMethod.Post, _AnalyticsEndPoint)
            {
                Headers =
                {
                     { "X-Environment-Key", _EnvironmentKey }
                },
                Content = new StringContent(JsonConvert.SerializeObject(AnalyticsData))
            };
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(_TimeOut));
            await _HttpClient.SendAsync(request, new CancellationTokenSource().Token);
            AnalyticsData.Clear();
            _LastFlushed = DateTime.Now;
        }
        /// <summary>
        /// Send analytics to server about feature usage.
        /// </summary>
        /// <param name="featureId"></param>
        /// <returns></returns>
        public async Task TrackFeature(int featureId)
        {
            AnalyticsData[featureId] = AnalyticsData.TryGetValue(featureId, out int value) ? value + 1 : 1;
            if ((DateTime.Now - _LastFlushed).Seconds > AnalyticsTimer)
                await Flush();
        }
    }
}
