using Flagsmith.Interfaces;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Trait.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IEngine _engine;
        private readonly IAnalyticsCollector _analytics;
        private readonly IEnvironmentAccessor _environmentAccessor;

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        public LocalEvalFlagsmithClient(ILogger<LocalEvalFlagsmithClient> logger, IAnalyticsCollector analytics, IFlagsmithClientConfig config, IEnvironmentAccessor environmentAccessor, IEngine engine)
        {
            _logger = logger;
            _config = config;
            _environmentAccessor = environmentAccessor;
            _engine = engine;
            _analytics = analytics;
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public Task<IFlags> GetEnvironmentFlags()
        {
            return Task.FromResult<IFlags>(Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(_environmentAccessor.GetEnvironment())));
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
        public Task<IFlags> GetIdentityFlags(string identity, IEnumerable<ITrait> traits)
        {
            var id = GetIdentity(identity, traits);
            return Task.FromResult<IFlags>(Flags.FromFeatureStateModel(_analytics, _config.DefaultFlagHandler, _engine.GetIdentityFeatureStates(_environmentAccessor.GetEnvironment(), id), id.CompositeKey));
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

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier, IEnumerable<ITrait> traits)
        {
            var segmentModels = Evaluator.GetIdentitySegments(_environmentAccessor.GetEnvironment(), GetIdentity(identifier, traits), new List<TraitModel>());
            return Task.FromResult<IReadOnlyCollection<ISegment>>(segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList());
        }
    }
}
