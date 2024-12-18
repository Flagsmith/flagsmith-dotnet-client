﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flagsmith.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class AnalyticsProcessor : IAnalyticsProcessor
    {
        private static AnalyticsProcessor _Instance;
        private static readonly object _staticLock = new object();

        private int _FlushIntervalSeconds = 10;
        private readonly DisposableLock _lock = new DisposableLock();
        private readonly string _AnalyticsEndPoint;
        private readonly string _EnvironmentKey;
        private readonly int _TimeOut;
        private DateTime _LastFlushed;
        private HttpClient _HttpClient;
        private ILogger _Logger;
        private Dictionary<string, string> _CustomHeaders;
        private ConcurrentDictionary<string, Dictionary<string, int>> AnalyticsDataThreads;

        public AnalyticsProcessor(HttpClient httpClient, string environmentKey, string baseApiUrl, ILogger logger = null, Dictionary<string, string> customHeaders = null, int timeOut = 3, int flushIntervalSeconds = 10)
        {
            _EnvironmentKey = environmentKey;
            _AnalyticsEndPoint = baseApiUrl + "analytics/flags/";
            _TimeOut = timeOut;
            _LastFlushed = DateTime.UtcNow;
            _HttpClient = httpClient;
            _Logger = logger;
            _FlushIntervalSeconds = flushIntervalSeconds;
            _CustomHeaders = customHeaders;
            AnalyticsDataThreads = new ConcurrentDictionary<string, Dictionary<string, int>>();
        }

        public static AnalyticsProcessor GetInstance(HttpClient httpClient, string environmentKey, string baseApiUrl, ILogger logger = null, Dictionary<string, string> customHeaders = null, int timeOut = 3, int flushIntervalSeconds = 10)
        {
            lock (_staticLock)
            {
                if (_Instance == null)
                {
                    _Instance = new AnalyticsProcessor(httpClient, environmentKey, baseApiUrl, logger, customHeaders, timeOut, flushIntervalSeconds);
                }

                return _Instance;
            }
        }

        /// <summary>
        /// Post the features on the provided endpoint and clear the cached data.
        /// </summary>
        /// <returns></returns>
        public async Task Flush()
        {
            using (_lock.AcquireLock())
                await FlushWithoutLock().ConfigureAwait(false);
        }

        private async Task FlushWithoutLock()
        {
            if (AnalyticsDataThreads.IsEmpty)
                return;

            try
            {
                var analyticsJson = JsonConvert.SerializeObject(GetAggregatedAnalyticsWithoutLock());
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
                var response = await _HttpClient.SendAsync(request, tokenSource.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                _Logger?.LogInformation("Analytics posted: " + analyticsJson);
                AnalyticsDataThreads.Clear();
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
            _LastFlushed = DateTime.UtcNow;
        }

        /// <summary>
        /// Record analytics about feature usage and call Flush() to send them to the server after the configured time interval.
        /// This implementation supports multi-threading and parallel processing by storing the analytics data in a separated Dictionary per thread.
        /// </summary>
        /// <param name="featureId"></param>
        /// <returns></returns>
        public async Task TrackFeature(string featureName)
        {
            using (_lock.AcquireLock())
            {
                string threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                // Get thread-specific count dictionary
                Dictionary<string, int> threadAnalyticsData;
                if (!AnalyticsDataThreads.TryGetValue(threadId, out threadAnalyticsData))
                {
                    threadAnalyticsData = new Dictionary<string, int>();
                    AnalyticsDataThreads[threadId] = threadAnalyticsData;
                }

                // Increment local thread count of the feature.
                int count;
                if (!threadAnalyticsData.TryGetValue(featureName, out count))
                {
                    count = 0;
                }
                count++;
                threadAnalyticsData[featureName] = count;

                var lastFlushedInterval = (DateTime.UtcNow - _LastFlushed).Seconds;

                if (lastFlushedInterval > _FlushIntervalSeconds)
                    await FlushWithoutLock().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets aggregated analytics data.
        /// This method is thread safe.
        /// It will aggregate the analytics data from all threads registered in AnalyticsDataThreads.
        /// </summary>
        /// <returns>Dictionary of feature name and usage count</returns>
        public Dictionary<string, int> GetAggregatedAnalytics()
        {
            using (_lock.AcquireLock())
            {
                return GetAggregatedAnalyticsWithoutLock();
            }
        }

        private Dictionary<string, int> GetAggregatedAnalyticsWithoutLock()
        {
            Dictionary<string, int> aggregatedAnalytics = new Dictionary<string, int>();
            foreach (var threadAnalyticsData in AnalyticsDataThreads.Values)
            {
                foreach (var trackedFeatureData in threadAnalyticsData)
                {
                    int count;
                    if (aggregatedAnalytics.TryGetValue(trackedFeatureData.Key, out count))
                    {
                        aggregatedAnalytics[trackedFeatureData.Key] = count + trackedFeatureData.Value;
                    }
                    else
                    {
                        aggregatedAnalytics[trackedFeatureData.Key] = trackedFeatureData.Value;
                    }
                }
            }
            return aggregatedAnalytics;
        }
    }
}
