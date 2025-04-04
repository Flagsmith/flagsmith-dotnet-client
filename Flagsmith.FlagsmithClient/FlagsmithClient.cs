#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flagsmith.Cache;
using Flagsmith.Extensions;
using Flagsmith.Providers;
using FlagsmithEngine;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfflineHandler;

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
        private EnvironmentModel? Environment { get; set; }
        private Dictionary<string, IdentityModel>? IdentitiesWithOverridesByIdentifier { get; set; }

        private readonly FlagsmithConfiguration _config;
        private readonly IEngine _engine = new Engine();
        private PollingManager? _pollingManager;
        private AnalyticsProcessor? _analyticsProcessor;
        private RegularFlagListCache? _regularFlagListCache;
        private ConcurrentDictionary<string, IdentityFlagListCache>? _flagListCacheDictionary;

        private void Initialise()
        {
            if (_config.OfflineMode && _config.OfflineHandler is null)
            {
                throw new Exception("ValueError: offlineHandler must be provided to use offline mode.");
            }
            else if (_config.DefaultFlagHandler != null && _config.OfflineHandler != null)
            {
                throw new Exception("ValueError: Cannot use both defaultFlagHandler and offlineHandler.");
            }

            if (_config.OfflineHandler != null)
            {
                Environment = _config.OfflineHandler.GetEnvironment();
            }

            if (!_config.OfflineMode)
            {
                if (string.IsNullOrEmpty(_config.EnvironmentKey))
                {
                    throw new Exception("ValueError: environmentKey is required");
                }
                if (_config.EnableAnalytics)
                    _analyticsProcessor = new AnalyticsProcessor(_config.HttpClient, _config.EnvironmentKey, _config.ApiUri.ToString(), _config.Logger, _config.CustomHeaders);

                if (_config.EnableLocalEvaluation)
                {
                    if (!_config.EnvironmentKey!.StartsWith("ser."))
                    {
                        Console.WriteLine(
                            "In order to use local evaluation, please generate a server key in the environment settings page."
                        );
                    }

                    _pollingManager = new PollingManager(GetAndUpdateEnvironmentFromApi, _config.EnvironmentRefreshInterval);
                    Task.Run(async () => await _pollingManager.StartPoll()).GetAwaiter().GetResult();
                }
            }

            if (_config.CacheConfig.Enabled)
            {
                _regularFlagListCache = new RegularFlagListCache(new DateTimeProvider(),
                    _config.CacheConfig.DurationInMinutes);
                _flagListCacheDictionary = new ConcurrentDictionary<string, IdentityFlagListCache>();
            }
        }

        public FlagsmithClient(FlagsmithConfiguration configuration)
        {
            _config = configuration;
            Initialise();
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
        {
            if (_config.CacheConfig.Enabled)
            {
                return _regularFlagListCache!.GetLatestFlags(GetFeatureFlagsFromCorrectSource);
            }

            return await GetFeatureFlagsFromCorrectSource().ConfigureAwait(false);
        }

        private async Task<IFlags> GetFeatureFlagsFromCorrectSource()
        {
            return (_config.OfflineMode || _config.EnableLocalEvaluation) && Environment != null ? GetFeatureFlagsFromDocument() : await GetFeatureFlagsFromApi().ConfigureAwait(false);
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identifier)
        {
            return await GetIdentityFlags(identifier, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        public async Task<IFlags> GetIdentityFlags(string identifier, List<ITrait>? traits, bool transient = false)
        {
            var identityWrapper = new IdentityWrapper(identifier, traits, transient);

            if (_config.CacheConfig.Enabled)
            {
                var flagListCache = GetFlagListCacheByIdentity(identityWrapper);

                return flagListCache.GetLatestFlags(GetIdentityFlagsFromCorrectSource);
            }

            if (_config.OfflineMode)
                return this.GetIdentityFlagsFromDocument(identifier, traits ?? null);

            return await GetIdentityFlagsFromCorrectSource(identityWrapper).ConfigureAwait(false);
        }

        public async Task<IFlags> GetIdentityFlagsFromCorrectSource(IdentityWrapper identityWrapper)
        {
            if ((_config.OfflineMode || _config.EnableLocalEvaluation) && Environment != null)
            {
                return GetIdentityFlagsFromDocument(identityWrapper.Identifier, identityWrapper.Traits);
            }

            return await GetIdentityFlagsFromApi(identityWrapper.Identifier, identityWrapper.Traits, identityWrapper.Transient).ConfigureAwait(false);
        }

        public List<ISegment>? GetIdentitySegments(string identifier)
        {
            return GetIdentitySegments(identifier, new List<ITrait>());
        }

        public List<ISegment>? GetIdentitySegments(string identifier, List<ITrait> traits)
        {
            if (Environment == null)
            {
                throw new FlagsmithClientError("Local evaluation required to obtain identity segments.");
            }

            IdentityModel identityModel = new IdentityModel { Identifier = identifier, IdentityTraits = traits?.Select(t => new TraitModel { TraitKey = t.GetTraitKey(), TraitValue = t.GetTraitValue() }).ToList() };
            List<SegmentModel> segmentModels = Evaluator.GetIdentitySegments(Environment, identityModel, new List<TraitModel>());

            return segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList<ISegment>();
        }

        private IdentityFlagListCache GetFlagListCacheByIdentity(IdentityWrapper identityWrapper)
        {
            var flagListCache = _flagListCacheDictionary!.GetOrAdd(identityWrapper.CacheKey, (key) =>
            {
                return new IdentityFlagListCache(identityWrapper,
                    new DateTimeProvider(),
                    _config.CacheConfig.DurationInMinutes);
            });

            return flagListCache;
        }

        private async Task<string> GetJson(HttpMethod method, string url, string? body = null)
        {
            try
            {
                var policy = HttpPolicies.GetRetryPolicyAwaitable(_config.Retries);
                return await (await policy.ExecuteAsync(async () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, url)
                    {
                        Headers =
                        {
                            { "X-Environment-Key", _config.EnvironmentKey }
                        }
                    };
                    _config.CustomHeaders?.ForEach(kvp => request.Headers.Add(kvp.Key, kvp.Value));
                    if (body != null)
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }

                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_config.RequestTimeout ?? 100));
                    HttpResponseMessage response = await _config.HttpClient.SendAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);
                    return response.EnsureSuccessStatusCode();
                }).ConfigureAwait(false)).Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                _config.Logger?.LogError("\nHTTP Request Exception Caught!");
                _config.Logger?.LogError("Message :{0} ", e.Message);
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
                var json = await GetJson(HttpMethod.Get, new Uri(_config.ApiUri, "environment-document/").AbsoluteUri).ConfigureAwait(false);
                Environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                IdentitiesWithOverridesByIdentifier = Environment?.IdentityOverrides != null ? Environment.IdentityOverrides.ToDictionary(identity => identity.Identifier) : new Dictionary<string, IdentityModel>();
                _config.Logger?.LogInformation("Local Environment updated: " + json);
            }
            catch (FlagsmithAPIError ex)
            {
                _config.Logger?.LogError(ex.Message);
            }
        }

        private async Task<IFlags> GetFeatureFlagsFromApi()
        {
            try
            {
                string url = new Uri(_config.ApiUri, "flags/").AbsoluteUri;
                string json = await GetJson(HttpMethod.Get, url).ConfigureAwait(false);
                var flags = JsonConvert.DeserializeObject<List<Flag>>(json)?.ToList<IFlag>();
                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (Environment != null)
                {
                    return this.GetFeatureFlagsFromDocument();
                }
                return _config.DefaultFlagHandler != null ? Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, null) : throw e;
            }
        }

        private async Task<IFlags> GetIdentityFlagsFromApi(string identity, List<ITrait> traits, bool transient = false)
        {
            try
            {
                traits = traits ?? new List<ITrait>();
                var url = new Uri(_config.ApiUri, "identities/").AbsoluteUri;
                var jsonBody = JsonConvert.SerializeObject(new { identifier = identity, traits, transient });
                var jsonResponse = await GetJson(HttpMethod.Post, url, body: jsonBody).ConfigureAwait(false);
                var flags = JsonConvert.DeserializeObject<Identity>(jsonResponse)?.flags?.ToList<IFlag>();

                return Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (Environment != null)
                {
                    return this.GetIdentityFlagsFromDocument(identity, traits);
                }
                return _config.DefaultFlagHandler != null ? Flags.FromApiFlag(_analyticsProcessor, _config.DefaultFlagHandler, null) : throw e;
            }
        }

        private IFlags GetFeatureFlagsFromDocument()
        {
            return Flags.FromFeatureStateModel(_analyticsProcessor, _config.DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(Environment));
        }

        private IFlags GetIdentityFlagsFromDocument(string identifier, List<ITrait>? traits)
        {
            List<TraitModel> traitModels = traits?.Count > 0
                ? traits.Select(t => new TraitModel { TraitKey = t.GetTraitKey(), TraitValue = t.GetTraitValue() }).ToList()
                : new List<TraitModel>();

            if (IdentitiesWithOverridesByIdentifier?.TryGetValue(identifier, out var identity) ?? false)
            {
                identity.UpdateTraits(traitModels);
            }
            else
            {
                identity = new IdentityModel
                {
                    EnvironmentApiKey = Environment!.ApiKey,
                    Identifier = identifier,
                    IdentityTraits = traitModels,
                };
            }
            return Flags.FromFeatureStateModel(_analyticsProcessor, _config.DefaultFlagHandler, _engine.GetIdentityFeatureStates(Environment, identity), identity.CompositeKey);
        }

        public Dictionary<string, int> aggregatedAnalytics => _analyticsProcessor != null ? _analyticsProcessor.GetAggregatedAnalytics() : new Dictionary<string, int>();

        ~FlagsmithClient() => _pollingManager?.StopPoll();
    }
}
