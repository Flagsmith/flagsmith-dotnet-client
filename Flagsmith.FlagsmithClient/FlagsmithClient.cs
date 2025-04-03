#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private string? ApiUrl { get; set; }
        private string? EnvironmentKey { get; set; }
        private bool EnableClientSideEvaluation { get; set; }
        private int EnvironmentRefreshIntervalSeconds { get; set; }
        private Func<string, IFlag>? DefaultFlagHandler { get; set; }
        private ILogger? Logger { get; set; }
        private bool EnableAnalytics { get; set; }
        private double? RequestTimeout { get; set; }
        private int Retries { get; set; }
        private CacheConfig CacheConfig { get; set; }
        private Dictionary<string, string>? CustomHeaders { get; set; }
        private EnvironmentModel? Environment { get; set; }
        private Dictionary<string, IdentityModel>? IdentitiesWithOverridesByIdentifier { get; set; }
        private bool OfflineMode { get; set; }
        const string DefaultApiUrl = "https://edge.api.flagsmith.com/api/v1/";

        private readonly HttpClient _httpClient;
        private readonly PollingManager? _pollingManager;
        private readonly IEngine _engine;
        private readonly AnalyticsProcessor? _analyticsProcessor;
        private readonly RegularFlagListCache? _regularFlagListCache; private readonly ConcurrentDictionary<string, IdentityFlagListCache>? _flagListCacheDictionary;
        private readonly BaseOfflineHandler? _offlineHandler;

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        /// <param name="environmentKey">The environment key obtained from Flagsmith interface. Required unless offlineMode is True</param>
        /// <param name="apiUrl">Override the URL of the Flagsmith API to communicate with. Required unless offlineMode is True</param>
        /// <param name="logger">Provide logger for logging polling info & errors which is only applicable when client side evalution is enabled and analytics errors.</param>
        /// <param name="defaultFlagHandler">Callable which will be used in the case where flags cannot be retrieved from the API or a non existent feature is requested.</param>
        /// <param name="enableAnalytics">if enabled, sends additional requests to the Flagsmith API to power flag analytics charts.</param>
        /// <param name="enableClientSideEvaluation">If using local evaluation, specify the interval period between refreshes of local environment data.</param>
        /// <param name="environmentRefreshIntervalSeconds"></param>
        /// <param name="customHeaders">Additional headers to add to requests made to the Flagsmith API</param>
        /// <param name="retries">Total http retries for every failing request before throwing the final error.</param>
        /// <param name="requestTimeout">Number of seconds to wait for a request to complete before terminating the request</param>
        /// <param name="httpClient">Http client used for flagsmith-API requests</param>
        /// <param name="cacheConfig">Cache configuration. Example new CacheConfig(true) </param>
        /// <param name="OfflineMode">Sets the client into offline mode. Relies on offlineHandler for evaluating flags.</param>
        /// <param name="offlineHandler">Offline handler for evaluating flags. Required unless OfflineMode is False.</param>
        /// <exception cref="FlagsmithAPIError">
        /// Thrown when error occurs during any http request to Flagsmith api.Not applicable for polling or ananlytics.
        /// </exception>
        /// <exception cref="FlagsmithClientError">
        /// A general exception with a error message. Example: Feature not found, etc.
        /// </exception>
        /// <exception cref="FlagsmithClientOfflineError">
        /// A general exception with a error message. Example: Feature not found, etc.
        /// </exception>

        public FlagsmithClient(
            string? environmentKey = null,
            string apiUrl = DefaultApiUrl,
            ILogger? logger = null,
            Func<string, IFlag>? defaultFlagHandler = null,
            bool enableAnalytics = false,
            bool enableClientSideEvaluation = false,
            int environmentRefreshIntervalSeconds = 60,
            Dictionary<string, string>? customHeaders = null,
            int retries = 1,
            double? requestTimeout = null,
            HttpClient? httpClient = null,
            CacheConfig? cacheConfig = null,
            bool offlineMode = false,
            BaseOfflineHandler? offlineHandler = null
            )
        {
            this.EnvironmentKey = environmentKey;
            this.Logger = logger;
            this.DefaultFlagHandler = defaultFlagHandler;
            this.EnableAnalytics = enableAnalytics;
            this.EnableClientSideEvaluation = enableClientSideEvaluation;
            this.EnvironmentRefreshIntervalSeconds = environmentRefreshIntervalSeconds;
            this.CustomHeaders = customHeaders;
            this.Retries = retries;
            this.RequestTimeout = requestTimeout;
            this._httpClient = httpClient ?? new HttpClient();
            this.CacheConfig = cacheConfig ?? new CacheConfig(false);
            this.OfflineMode = offlineMode;
            this._offlineHandler = offlineHandler;
            _engine = new Engine();

            if (OfflineMode && _offlineHandler is null)
            {
                throw new Exception("ValueError: offlineHandler must be provided to use offline mode.");
            }
            else if (DefaultFlagHandler != null && _offlineHandler != null)
            {
                throw new Exception("ValueError: Cannot use both defaultFlagHandler and offlineHandler.");
            }

            if (_offlineHandler != null)
            {
                Environment = _offlineHandler.GetEnvironment();
            }

            if (!OfflineMode)
            {
                if (string.IsNullOrEmpty(EnvironmentKey))
                {
                    throw new Exception("ValueError: environmentKey is required");
                }

                var _apiUrl = apiUrl ?? DefaultApiUrl;
                ApiUrl = _apiUrl.EndsWith("/") ? _apiUrl : $"{_apiUrl}/";

                if (EnableAnalytics)
                    _analyticsProcessor = new AnalyticsProcessor(this._httpClient, EnvironmentKey, ApiUrl, Logger, CustomHeaders);

                if (EnableClientSideEvaluation)
                {
                    if (!EnvironmentKey!.StartsWith("ser."))
                    {
                        throw new Exception(
                            "ValueError: In order to use local evaluation, please generate a server key in the environment settings page."
                        );
                    }

                    _pollingManager = new PollingManager(GetAndUpdateEnvironmentFromApi, EnvironmentRefreshIntervalSeconds);
                    Task.Run(async () => await _pollingManager.StartPoll()).GetAwaiter().GetResult();
                }
            }

            if (CacheConfig.Enabled)
            {
                _regularFlagListCache = new RegularFlagListCache(new DateTimeProvider(),
                    CacheConfig.DurationInMinutes);
                _flagListCacheDictionary = new ConcurrentDictionary<string, IdentityFlagListCache>();
            }
        }

        /// <summary>
        /// Create flagsmith client.
        /// </summary>
        /// <param name="configuration">Flagsmith client configuration</param>
        /// <param name="httpClient">Http client used for flagsmith-API requests</param>
        public FlagsmithClient(IFlagsmithConfiguration configuration, HttpClient? httpClient = null) : this(
            configuration.EnvironmentKey,
            configuration.ApiUrl,
            configuration.Logger,
            configuration.DefaultFlagHandler,
            configuration.EnableAnalytics,
            configuration.EnableClientSideEvaluation,
            configuration.EnvironmentRefreshIntervalSeconds,
            configuration.CustomHeaders,
            configuration.Retries ?? 1,
            configuration.RequestTimeout,
            httpClient,
            configuration.CacheConfig)
        {
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<IFlags> GetEnvironmentFlags()
        {
            if (CacheConfig.Enabled)
            {
                return _regularFlagListCache!.GetLatestFlags(GetFeatureFlagsFromCorrectSource);
            }

            return await GetFeatureFlagsFromCorrectSource().ConfigureAwait(false);
        }

        private async Task<IFlags> GetFeatureFlagsFromCorrectSource()
        {
            return (OfflineMode || EnableClientSideEvaluation) && Environment != null ? GetFeatureFlagsFromDocument() : await GetFeatureFlagsFromApi().ConfigureAwait(false);
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

            if (CacheConfig.Enabled)
            {
                var flagListCache = GetFlagListCacheByIdentity(identityWrapper);

                return flagListCache.GetLatestFlags(GetIdentityFlagsFromCorrectSource);
            }

            if (this.OfflineMode)
                return this.GetIdentityFlagsFromDocument(identifier, traits ?? null);

            return await GetIdentityFlagsFromCorrectSource(identityWrapper).ConfigureAwait(false);
        }

        public async Task<IFlags> GetIdentityFlagsFromCorrectSource(IdentityWrapper identityWrapper)
        {
            if ((OfflineMode || EnableClientSideEvaluation) && Environment != null)
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
            if (this.Environment == null)
            {
                throw new FlagsmithClientError("Local evaluation required to obtain identity segments.");
            }

            IdentityModel identityModel = new IdentityModel { Identifier = identifier, IdentityTraits = traits?.Select(t => new TraitModel { TraitKey = t.GetTraitKey(), TraitValue = t.GetTraitValue() }).ToList() };
            List<SegmentModel> segmentModels = Evaluator.GetIdentitySegments(this.Environment, identityModel, new List<TraitModel>());

            return segmentModels?.Select(t => new Segment(id: t.Id, name: t.Name)).ToList<ISegment>();
        }

        private IdentityFlagListCache GetFlagListCacheByIdentity(IdentityWrapper identityWrapper)
        {
            var flagListCache = _flagListCacheDictionary!.GetOrAdd(identityWrapper.CacheKey, (key) =>
            {
                return new IdentityFlagListCache(identityWrapper,
                    new DateTimeProvider(),
                    CacheConfig.DurationInMinutes);
            });

            return flagListCache;
        }

        private async Task<string> GetJson(HttpMethod method, string url, string? body = null)
        {
            try
            {
                var policy = HttpPolicies.GetRetryPolicyAwaitable(Retries);
                return await (await policy.ExecuteAsync(async () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, url)
                    {
                        Headers =
                        {
                            { "X-Environment-Key", EnvironmentKey }
                        }
                    };
                    CustomHeaders?.ForEach(kvp => request.Headers.Add(kvp.Key, kvp.Value));
                    if (body != null)
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }

                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeout ?? 100));
                    HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);
                    return response.EnsureSuccessStatusCode();
                }).ConfigureAwait(false)).Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                Logger?.LogError("\nHTTP Request Exception Caught!");
                Logger?.LogError("Message :{0} ", e.Message);
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
                var json = await GetJson(HttpMethod.Get, ApiUrl + "environment-document/").ConfigureAwait(false);
                Environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                IdentitiesWithOverridesByIdentifier = Environment?.IdentityOverrides != null ? Environment.IdentityOverrides.ToDictionary(identity => identity.Identifier) : new Dictionary<string, IdentityModel>();
                Logger?.LogInformation("Local Environment updated: " + json);
            }
            catch (FlagsmithAPIError ex)
            {
                Logger?.LogError(ex.Message);
            }
        }

        private async Task<IFlags> GetFeatureFlagsFromApi()
        {
            try
            {
                string url = ApiUrl.AppendPath("flags");
                string json = await GetJson(HttpMethod.Get, url).ConfigureAwait(false);
                var flags = JsonConvert.DeserializeObject<List<Flag>>(json)?.ToList<IFlag>();
                return Flags.FromApiFlag(_analyticsProcessor, DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (Environment != null)
                {
                    return this.GetFeatureFlagsFromDocument();
                }
                return DefaultFlagHandler != null ? Flags.FromApiFlag(_analyticsProcessor, DefaultFlagHandler, null) : throw e;
            }
        }

        private async Task<IFlags> GetIdentityFlagsFromApi(string identity, List<ITrait> traits, bool transient = false)
        {
            try
            {
                traits = traits ?? new List<ITrait>();
                var url = ApiUrl.AppendPath("identities");
                var jsonBody = JsonConvert.SerializeObject(new { identifier = identity, traits, transient });
                var jsonResponse = await GetJson(HttpMethod.Post, url, body: jsonBody).ConfigureAwait(false);
                var flags = JsonConvert.DeserializeObject<Identity>(jsonResponse)?.flags?.ToList<IFlag>();

                return Flags.FromApiFlag(_analyticsProcessor, DefaultFlagHandler, flags);
            }
            catch (FlagsmithAPIError e)
            {
                if (Environment != null)
                {
                    return this.GetIdentityFlagsFromDocument(identity, traits);
                }
                return DefaultFlagHandler != null ? Flags.FromApiFlag(_analyticsProcessor, DefaultFlagHandler, null) : throw e;
            }
        }

        private IFlags GetFeatureFlagsFromDocument()
        {
            return Flags.FromFeatureStateModel(_analyticsProcessor, DefaultFlagHandler, _engine.GetEnvironmentFeatureStates(Environment));
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
            return Flags.FromFeatureStateModel(_analyticsProcessor, DefaultFlagHandler, _engine.GetIdentityFeatureStates(Environment, identity), identity.CompositeKey);
        }

        public Dictionary<string, int> aggregatedAnalytics => _analyticsProcessor != null ? _analyticsProcessor.GetAggregatedAnalytics() : new Dictionary<string, int>();

        ~FlagsmithClient() => _pollingManager?.StopPoll();
    }
}
