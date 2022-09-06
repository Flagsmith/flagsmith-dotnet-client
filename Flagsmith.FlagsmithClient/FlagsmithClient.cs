using Flagsmith.Extensions;
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
        private readonly ILogger _logger;
        private readonly IFlagsmithClientConfig _config;
        private readonly HttpClient _httpClient;
        private readonly PollingManager _pollingManager;
        private readonly IEngine _engine;
        private readonly AnalyticsProcessor _analyticsProcessor;
        private EnvironmentModel _environment;

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        public FlagsmithClient(ILogger<FlagsmithClient> logger, IFlagsmithClientConfig config, HttpClient client = null)
        {
            _logger = logger;
            _config = config;
            _httpClient = client ?? new HttpClient();
            _engine = new Engine();

            if (_config.EnableAnalytics)
                _analyticsProcessor = new AnalyticsProcessor(_httpClient, _config.EnvironmentKey, _config.ApiUrl, _logger, _config.CustomHeaders);

            if (_config.EnableClientSideEvaluation)
            {
                _pollingManager = new PollingManager(GetAndUpdateEnvironmentFromApi, _config.EnvironmentRefreshIntervalSeconds);
                _ = _pollingManager.StartPoll();
            }
        }

        ~FlagsmithClient()
        {
            _pollingManager?.StopPoll();
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
            => _environment != null ? GetFeatureFlagsFromDocuments() : await GetFeatureFlagsFromApi();

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
            if (_environment != null)
            {
                return GetIdentityFlagsFromDocuments(identity, traits);
            }
            return await GetIdentityFlagsFromApi(identity, traits);
        }

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier)
        {
            return GetIdentitySegments(identifier, Array.Empty<ITrait>());
        }

        public Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier, IEnumerable<ITrait> traits)
        {
            if (_environment == null)
            {
                throw new FlagsmithClientError("Local evaluation required to obtain identity segments.");
            }
            var identityModel = new IdentityModel { Identifier = identifier, IdentityTraits = traits?.Select(t => new TraitModel { TraitKey = t.Key, TraitValue = t.Value }).ToList() };
            var segmentModels = Evaluator.GetIdentitySegments(_environment, identityModel, new List<TraitModel>());
            return Task.FromResult<IReadOnlyCollection<ISegment>>(segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList());
        }

        private async Task<string> GetJSON(HttpMethod method, string url, string body = null)
        {
            try
            {
                var policy = HttpPolicies.GetRetryPolicyAwaitable(_config.Retries);
                return await (await policy.ExecuteAsync(async () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, url)
                    {
                        Headers = {
                            { "X-Environment-Key", _config.EnvironmentKey }
                        }
                    };
                    _config.CustomHeaders?.ForEach(kvp => request.Headers.Add(kvp.Key, kvp.Value));
                    if (body != null)
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_config.RequestTimeout ?? 100));
                    HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationTokenSource.Token);
                    return response.EnsureSuccessStatusCode();
                })).Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                _logger?.LogError("\nHTTP Request Exception Caught!");
                _logger?.LogError("Message :{0} ", e.Message);
                throw new FlagsmithAPIError("Unable to get valid response from Flagsmith API");
            }
            catch (TaskCanceledException)
            {
                throw new FlagsmithAPIError("Request cancelled: Api server takes too long to respond");
            }
        }

        private async Task GetAndUpdateEnvironmentFromApi()
        {
            try
            {
                var json = await GetJSON(HttpMethod.Get, _config.ApiUrl + "environment-document/");
                _environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                _logger?.LogInformation("Local Environment updated: " + json);
            }
            catch (FlagsmithAPIError ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        private async Task<Flags> GetFeatureFlagsFromApi()
        {
            try
            {
                string url = _config.ApiUrl.AppendPath("flags");
                string json = await GetJSON(HttpMethod.Get, url);
                var flags = JsonConvert.DeserializeObject<List<Flag>>(json);
                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (_config.DefaultFlagHandler == null)
                    throw;

                _logger?.LogError(e, "GetFeatureFlagsFromApi failed.");
                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, null);
            }
        }

        private async Task<Flags> GetIdentityFlagsFromApi(string identity, IEnumerable<ITrait> traits)
        {
            try
            {
                string url = _config.ApiUrl.AppendPath("identities");
                var jsonBody = JsonConvert.SerializeObject(new { identifier = identity, traits = traits ?? new List<Trait>() });
                string jsonResponse = await GetJSON(HttpMethod.Post, url, body: jsonBody);
                var flags = JsonConvert.DeserializeObject<Identity>(jsonResponse)?.flags;
                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (_config.DefaultFlagHandler == null)
                    throw;

                _logger?.LogError(e, "GetIdentityFlagsFromApi failed.");
                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, null);
            }
        }

        private Flags GetFeatureFlagsFromDocuments()
        {
            return Flags.FromFeatureStateModel(_analyticsProcessor, _config.DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(_environment));
        }

        private Flags GetIdentityFlagsFromDocuments(string identifier, IEnumerable<ITrait> traits)
        {
            IdentityModel identity;

            if (traits != null && traits.Any())
            {
                identity = new IdentityModel { Identifier = identifier, IdentityTraits = traits?.Select(t => new TraitModel { TraitKey = t.Key, TraitValue = t.Value }).ToList() };
            }
            else
            {
                identity = new IdentityModel { Identifier = identifier };
            }

            return Flags.FromFeatureStateModel(_analyticsProcessor, _config.DefaultFlagHandler, _engine.GetIdentityFeatureStates(_environment, identity), identity.CompositeKey);
        }
    }
}
