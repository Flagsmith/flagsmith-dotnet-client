using Flagsmith.Interfaces;
using FlagsmithEngine.Environment.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class EnvironmentRefreshService : BackgroundService, IEnvironmentAccessor
    {
        private EnvironmentModel _environment;
        private readonly IFlagsmithClientConfig _config;
        private readonly ILogger<EnvironmentRefreshService> _logger;
        private readonly IRestClient _restClient;

        public EnvironmentRefreshService(ILogger<EnvironmentRefreshService> logger, IFlagsmithClientConfig config, IRestClient restClient)
        {
            _logger = logger;
            _config = config;
            _restClient = restClient;
        }

        public EnvironmentModel GetEnvironmentModel()
        {
            if (_environment == null)
                throw new FlagsmithClientError("Failed to get local environment.");
            return _environment;
        }

        public async Task UpdateEnvironment(CancellationToken token)
        {
            try
            {
                var json = await _restClient.Send(HttpMethod.Get, "environment-document", null, token).ConfigureAwait(false);
                _environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                _logger.LogInformation("Local environment updated.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get local environment.");
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateEnvironment(cancellationToken).ConfigureAwait(false);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.EnvironmentRefreshIntervalSeconds), stoppingToken).ConfigureAwait(false);
                    await UpdateEnvironment(stoppingToken).ConfigureAwait(false);
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
