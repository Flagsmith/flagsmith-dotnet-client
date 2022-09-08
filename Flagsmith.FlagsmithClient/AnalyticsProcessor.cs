using Flagsmith.Extensions;
using Flagsmith.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class AnalyticsProcessor : BackgroundService, IAnalyticsCollector
    {
        private Dictionary<string, int> _data = new Dictionary<string, int>();
        private readonly object _sync = new object();
        private readonly IFlagsmithClientConfig _config;
        private readonly ILogger<AnalyticsProcessor> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AnalyticsProcessor(ILogger<AnalyticsProcessor> logger, IFlagsmithClientConfig config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Post the features on the provided endpoint and clear the cached data.
        /// </summary>
        /// <returns></returns>
        private async Task Flush(CancellationToken stoppingToken)
        {
            Dictionary<string, int> temp;
            lock (_sync)
            {
                if (!_data.Any())
                {
                    _logger.LogDebug("No statistics to be posted.");
                    return;
                }
                temp = _data;
                _data = new Dictionary<string, int>();
            }

            try
            {
                var client = _httpClientFactory.CreateClient(_config.ApiUrl + _config.EnvironmentKey);
                using (var request = new HttpRequestMessage(HttpMethod.Post, "analytics/flags/"))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(_data), Encoding.UTF8, "application/json");
                    using (var response = await client.SendAsync(request, stoppingToken))
                        response.EnsureSuccessStatusCode();
                }
                _logger.LogDebug("Statistics posted successfully.");
            }
            catch
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    lock (_sync)
                    {
                        var data = _data;
                        _data = temp;
                        data.ForEach(x => TrackFeatureInternal(x.Key, x.Value));
                    }
                }
                throw;
            }
        }

        private void TrackFeatureInternal(string name, int increment)
        {
            _data[name] = _data.TryGetValue(name, out var value) ? value + increment : increment;
        }

        /// <summary>
        /// Track feature usuage
        /// </summary>
        /// <param name="name">feature name</param>
        public void TrackFeature(string name)
        {
            lock (_sync)
                TrackFeatureInternal(name, 1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    try
                    {
                        await Flush(stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to flush the statistics.");
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _logger.LogDebug("Exiting...");
            }
        }
    }
}
