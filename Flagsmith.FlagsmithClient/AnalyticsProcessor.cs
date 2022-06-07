using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Flagsmith.Extensions;

namespace Flagsmith
{
    public class AnalyticsProcessor
    {
        int _FlushIntervalSeconds = 10;
        readonly string _AnalyticsEndPoint;
        readonly string _EnvironmentKey;
        readonly int _TimeOut;
        DateTime _LastFlushed;
        protected Dictionary<int, int> AnalyticsData;
        HttpClient _HttpClient;
        ILogger _Logger;
        Dictionary<string, string> _CustomHeaders;
        public AnalyticsProcessor(HttpClient httpClient, string environmentKey, string baseApiUrl, ILogger logger = null, Dictionary<string, string> customHeaders = null, int timeOut = 3, int flushIntervalSeconds = 10)
        {
            _EnvironmentKey = environmentKey;
            _AnalyticsEndPoint = baseApiUrl + "analytics/flags/";
            _TimeOut = timeOut;
            _LastFlushed = DateTime.Now;
            AnalyticsData = new Dictionary<int, int>();
            _HttpClient = httpClient;
            _Logger = logger;
            _FlushIntervalSeconds = flushIntervalSeconds;
            _CustomHeaders = customHeaders;
        }
        /// <summary>
        /// Post the features on the provided endpoint and clear the cached data.
        /// </summary>
        /// <returns></returns>
        public async Task Flush()
        {
            if (AnalyticsData?.Any() == false)
                return;
            try
            {
                var analyticsJson = JsonConvert.SerializeObject(AnalyticsData);
                var request = new HttpRequestMessage(HttpMethod.Post, _AnalyticsEndPoint)
                {
                    Headers =
                {
                    { "X-Environment-Key", _EnvironmentKey }
                },
                    Content = new StringContent(analyticsJson, Encoding.UTF8, "application/json")
                };
                _CustomHeaders?.ForEach(kvp => request.Headers.Add(kvp.Key, kvp.Value));
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(TimeSpan.FromSeconds(_TimeOut));
                var response = await _HttpClient.SendAsync(request, tokenSource.Token);
                response.EnsureSuccessStatusCode();
                _Logger?.LogInformation("Analytics posted: " + analyticsJson);
                AnalyticsData.Clear();
                _Logger?.LogInformation("Analytics cleared: " + analyticsJson);
            }
            catch (HttpRequestException ex)
            {
                _Logger?.LogError("Analytics api error: " + ex.Message);
            }
            catch (TaskCanceledException)
            {
                _Logger?.LogWarning("Analytics request cancelled: Api request takes too long to respond");
            }
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
            if ((DateTime.Now - _LastFlushed).Seconds > _FlushIntervalSeconds)
                await Flush();
        }
    }
}
