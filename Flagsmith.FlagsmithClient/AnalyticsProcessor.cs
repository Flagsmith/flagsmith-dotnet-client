using Flagsmith.Extensions;
using Flagsmith.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class AnalyticsProcessor : BackgroundService, IAnalyticsCollector
    {
        private Dictionary<string, int> _data = new Dictionary<string, int>();
        private readonly object _sync = new object();
        private readonly ILogger<AnalyticsProcessor> _logger;
        private readonly IRestClient _restClient;

        public AnalyticsProcessor(ILogger<AnalyticsProcessor> logger, IRestClient restClient)
        {
            _logger = logger;
            _restClient = restClient;
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
                await _restClient.Send(HttpMethod.Post, "analytics/flags", JsonConvert.SerializeObject(temp), stoppingToken).ConfigureAwait(false);
                _logger.LogDebug("Statistics posted successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to flush the statistics.");
                if (!stoppingToken.IsCancellationRequested)
                {
                    lock (_sync)
                    {
                        var data = _data;
                        _data = temp;
                        data.ForEach(x => TrackFeatureInternal(x.Key, x.Value));
                    }
                }
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
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken).ConfigureAwait(false);
                    await Flush(stoppingToken).ConfigureAwait(false);
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
