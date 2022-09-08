using Flagsmith.Caching;
using Flagsmith.Interfaces;
using FlagsmithEngine;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Trait.Models;
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
    /// <summary>
    /// A Flagsmith client.
    /// Provides an interface for interacting with the Flagsmith http API.
    /// </summary>
    /// <exception cref="FlagsmithAPIError">
    /// Thrown when error occurs during any http request to Flagsmith api.Not applicable for polling or ananlytics.
    /// </exception>
    /// <exception cref="FlagsmithClientError">
    /// A general exception with a error message. Example: Feature not found, etc.
    /// </exception>
    public class FlagsmithClient : IFlagsmithClient
    {
        private readonly ILogger<FlagsmithClient> _logger;
        private readonly IFlagsmithClientConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEngine _engine;
        private readonly ICache _cache;
        private readonly IAnalyticsCollector _analytics;

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        public FlagsmithClient(ILogger<FlagsmithClient> logger, ICache cache, IAnalyticsCollector analytics, IFlagsmithClientConfig config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _engine = new Engine();
            _analytics = analytics;
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
        {
            return Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(await GetEnvironment()));
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity)
        {
            return await GetIdentityFlags(identity, null);
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity, IEnumerable<ITrait> traits)
        {
            var id = GetIdentity(identity, traits);
            return Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetIdentityFeatureStates(await GetEnvironment(), id), id.CompositeKey);
        }

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier)
        {
            return GetIdentitySegments(identifier, Array.Empty<ITrait>());
        }

        private IdentityModel GetIdentity(string identity, IEnumerable<ITrait> traits)
        {
            IdentityModel id;
            if (traits != null && traits.Any())
                id = new IdentityModel { Identifier = identity, IdentityTraits = traits.Select(t => new TraitModel { TraitKey = t.Key, TraitValue = t.Value }).ToList() };
            else
                id = new IdentityModel { Identifier = identity };
            return id;
        }

        public async Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier, IEnumerable<ITrait> traits)
        {
            var segmentModels = Evaluator.GetIdentitySegments(await GetEnvironment(), GetIdentity(identifier, traits), new List<TraitModel>());
            return segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList();
        }

        private async Task<string> GetJSON(HttpMethod method, string url, string body = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(_config.ApiUrl + _config.EnvironmentKey);

                var policy = HttpPolicies.GetRetryPolicyAwaitable(_config.Retries);
                using var response = await policy.ExecuteAsync(async () =>
                {
                    using var request = new HttpRequestMessage(method, url);
                    if (!string.IsNullOrEmpty(body))
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_config.RequestTimeout ?? 100));
                    var response = await client.SendAsync(request, cancellationTokenSource.Token);
                    return response.EnsureSuccessStatusCode();
                });

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                _logger?.LogError(e, "\nHTTP Request Exception Caught!");
                throw new FlagsmithAPIError("Unable to get valid response from Flagsmith API", e);
            }
            catch (TaskCanceledException e)
            {
                _logger?.LogError(e, "\nHTTP Request Exception Caught!");
                throw new FlagsmithAPIError("Request cancelled: Api server takes too long to respond", e);
            }
        }

        private async Task<EnvironmentModel> GetEnvironment()
        {
            return await _cache.GetObjectAsync(_config.ApiUrl + _config.EnvironmentKey, async policy =>
            {
                policy.AbsoluteExpiration = DateTime.Now + TimeSpan.FromSeconds(_config.EnvironmentRefreshIntervalSeconds);

                var json = await GetJSON(HttpMethod.Get, "environment-document/");
                var env = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                _logger?.LogInformation("Local Environment updated: " + json);

                return env;
            });
        }
    }
}
