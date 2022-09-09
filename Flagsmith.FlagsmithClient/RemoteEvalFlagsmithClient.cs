using Flagsmith.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    public class RemoteEvalFlagsmithClient : IFlagsmithClient
    {
        private readonly ILogger<RemoteEvalFlagsmithClient> _logger;
        private readonly IFlagsmithClientConfig _config;
        private readonly IRestClient _restClient;
        private readonly IAnalyticsCollector _analytics;

        /// <summary>
        /// Create flagsmith client
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="analytics"></param>
        /// <param name="config"></param>
        /// <param name="restClient"></param>
        public RemoteEvalFlagsmithClient(ILogger<RemoteEvalFlagsmithClient> logger, IAnalyticsCollector analytics, IFlagsmithClientConfig config, IRestClient restClient)
        {
            _logger = logger;
            _config = config;
            _restClient = restClient;
            _analytics = analytics;
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
        {
            try
            {
                var json = await _restClient.Send(HttpMethod.Get, "flags", null, CancellationToken.None);
                var flags = JsonConvert.DeserializeObject<List<Flag>>(json);
                return Flags.FromApiFlag(_analytics, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (_config.DefaultFlagHandler != null)
                {
                    _logger?.LogWarning(e, "GetEnvironmentFlags() method failed.");
                    return Flags.FromApiFlag(_analytics, _config.DefaultFlagHandler, null);
                }
                throw;
            }
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity)
        {
            return await GetIdentityFlagsFromApi(identity, null);
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identity, IEnumerable<ITrait> traits)
        {
            return await GetIdentityFlagsFromApi(identity, traits);
        }

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier)
        {
            throw new NotSupportedException("Not supported in remote evaluation mode.");
        }

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier, IEnumerable<ITrait> traits)
        {
            throw new NotSupportedException("Not supported in remote evaluation mode.");
        }

        private async Task<Flags> GetIdentityFlagsFromApi(string identity, IEnumerable<ITrait> traits)
        {
            try
            {
                var jsonBody = JsonConvert.SerializeObject(new { identifier = identity, traits = traits ?? new List<Trait>() });
                var jsonResponse = await _restClient.Send(HttpMethod.Post, "identities", jsonBody, CancellationToken.None);
                var flags = JsonConvert.DeserializeObject<Identity>(jsonResponse)?.flags;
                return Flags.FromApiFlag(_analytics, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (_config.DefaultFlagHandler != null)
                {
                    _logger?.LogWarning(e, "GetIdentityFlagsFromApi() method failed.");
                    return Flags.FromApiFlag(_analytics, _config.DefaultFlagHandler, null);
                }
                throw;
            }
        }
    }
}
