using Flagsmith.Caching;
using Flagsmith.Interfaces;
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
    public class LocalEvalFlagsmithClient : IFlagsmithClient
    {
        private readonly ILogger<LocalEvalFlagsmithClient> _logger;
        private readonly IFlagsmithClientConfig _config;
        private readonly IRestClient _restClient;
        private readonly IEngine _engine;
        private readonly ICache _cache;
        private readonly IAnalyticsCollector _analytics;

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        public LocalEvalFlagsmithClient(ILogger<LocalEvalFlagsmithClient> logger, ICache cache, IAnalyticsCollector analytics, IFlagsmithClientConfig config, IRestClient restClient, IEngine engine)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _restClient = restClient;
            _engine = engine;
            _analytics = analytics;
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
        {
            return Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(await GetEnvironment().ConfigureAwait(false)));
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity)
        {
            return await GetIdentityFlags(identity, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity, IEnumerable<ITrait> traits)
        {
            var id = GetIdentity(identity, traits);
            return Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetIdentityFeatureStates(await GetEnvironment().ConfigureAwait(false), id), id.CompositeKey);
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
            var segmentModels = Evaluator.GetIdentitySegments(await GetEnvironment().ConfigureAwait(false), GetIdentity(identifier, traits), new List<TraitModel>());
            return segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList();
        }

        private async Task<EnvironmentModel> GetEnvironment()
        {
            return await _cache.GetObjectAsync(_config.ApiUrl + _config.EnvironmentKey, async policy =>
            {
                policy.AbsoluteExpiration = DateTime.Now + TimeSpan.FromSeconds(_config.EnvironmentRefreshIntervalSeconds);

                var json = await _restClient.Send(HttpMethod.Get, "environment-document", null, CancellationToken.None).ConfigureAwait(false);
                var env = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                _logger?.LogInformation("Local Environment updated: " + json);

                return env;
            }).ConfigureAwait(false);
        }
    }
}
