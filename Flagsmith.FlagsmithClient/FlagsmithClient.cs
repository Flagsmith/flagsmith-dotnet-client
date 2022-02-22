using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Trait.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using Flagsmith.Extensions;
using System.Linq;

namespace Flagsmith
{   /// <summary>
    /// A Flagsmith client.
    /// Provides an interface for interacting with the Flagsmith http API.
    /// </summary>
    /// <exception cref="FlagsmithAPIError">
    /// Thrown when error occurs during any http request to Flagsmith api.Not applicable for polling or ananlytics.
    /// </exception>
    /// <exception cref="FlagsmithClientError">
    /// A general exception with a error message. Example: Feature not found, etc.
    /// </exception>
    public class FlagsmithClient
    {
        public static FlagsmithClient instance;

        private readonly FlagsmithConfiguration configuration;
        protected static HttpClient httpClient;
        protected EnvironmentModel Environment { get; set; }
        private readonly PollingManager _PollingManager;
        private readonly IEngine _Engine;
        private readonly AnalyticsProcessor _AnalyticsProcessor;

        public FlagsmithClient(FlagsmithConfiguration flagsmithConfiguration)
        {
            if (flagsmithConfiguration == null)
            {
                throw new ArgumentNullException(nameof(flagsmithConfiguration),
                    "Parameter must be provided when constructing an instance of the client.");
            }

            if (!flagsmithConfiguration.IsValid())
            {
                throw new ArgumentException("The provided configuration is not valid. An API Url and Environment Key must be provided.", nameof(flagsmithConfiguration));
            }

            if (instance == null)
            {
                configuration = flagsmithConfiguration;
                httpClient = new HttpClient();
                instance = this;
                _Engine = new Engine();
                if (configuration.EnableAnalytics)
                    _AnalyticsProcessor = new AnalyticsProcessor(httpClient, configuration.EnvironmentKey, configuration.ApiUrl, configuration.Logger, configuration.CustomHeaders);
                if (configuration.EnableClientSideEvaluation)
                {
                    _PollingManager = new PollingManager(GetAndUpdateEnvironmentFromApi, configuration.EnvironmentRefreshIntervalSeconds);
                    _ = _PollingManager.StartPoll();
                }
            }
            else
            {
                throw new NotSupportedException("FlagsmithClient should only be initialised once. Use FlagsmithClient.instance after successful initialisation");
            }
        }

        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        public async Task<List<Flag>> GetFeatureFlags()
            => Environment != null ? GetFeatureFlagsFromDocuments() : await GetFeatureFlagsFromApi();

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>

        public async Task<List<Flag>> GetFeatureFlags(string identity, List<Trait> traits = null)
             => Environment != null ? GetIdentityFlagsFromDocuments(identity, traits) : await GetIdentityFlagsFromApi(identity);

        /// <summary>
        /// Check feature exists and is enabled optionally for a specific identity
        /// </summary>
        /// <returns>Null if Flagsmith is unaccessible</returns>
        public async Task<bool?> HasFeatureFlag(string featureName, string identity = null, List<Trait> traits = null)
        {
            List<Flag> flags = identity == null ? await GetFeatureFlags() : await GetFeatureFlags(identity, traits);
            if (flags == null)
            {
                return null;
            }

            foreach (Flag flag in flags)
            {
                if (flag.GetFeature().GetName().Equals(featureName) && flag.IsEnabled())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get remote config value optionally for a specific identity.
        /// </summary>
        public async Task<string> GetFeatureValue(string featureName, string identity = null, List<Trait> traits = null)
        {
            List<Flag> flags = null;
            try
            {
                flags = identity == null ? await GetFeatureFlags() : await GetFeatureFlags(identity, traits);
            }
            catch (FlagsmithAPIError)
            {
                var val = await configuration.DefaultFlagHandler?.Invoke(featureName)?.GetValue();
                if (val != null)
                    return val;
                throw;
            }

            if (flags != null)
            {
                foreach (Flag flag in flags)
                {
                    if (flag.GetFeature().GetName().Equals(featureName))
                    {
                        return await flag.GetValue();
                    }
                }
            }
            var value = await configuration.DefaultFlagHandler?.Invoke(featureName)?.GetValue();
            return value ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);
        }
        /// <summary>
        /// Get feature flag optionally for a specific identity.
        /// </summary>
        public async Task<Flag> GetFeatureFlag(string featureName, string identity = null, List<Trait> traits = null)
        {
            List<Flag> flags = null;
            try
            {
                flags = identity == null ? await GetFeatureFlags() : await GetFeatureFlags(identity, traits);
            }
            catch (FlagsmithAPIError)
            {
                var val = configuration.DefaultFlagHandler?.Invoke(featureName);
                if (val != null)
                    return val;
                throw;
            }

            if (flags != null)
            {
                foreach (Flag flag in flags)
                {
                    if (flag.GetFeature().GetName().Equals(featureName))
                    {
                        return flag;
                    }
                }
            }
            var value = configuration.DefaultFlagHandler?.Invoke(featureName);
            return value ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);
        }

        /// <summary>
        /// Get all user traits for provided identity. Optionally filter results with a list of keys
        /// </summary>
        public async Task<List<Trait>> GetTraits(string identity, List<string> keys = null)
        {
            try
            {
                string url = GetIdentitiesUrl(identity);
                string json = await GetJSON(HttpMethod.Get, url);

                List<Trait> traits = JsonConvert.DeserializeObject<Identity>(json)?.traits;
                if (traits == null)
                {
                    return null;
                }
                if (keys == null)
                {
                    return traits;
                }

                List<Trait> filteredTraits = new List<Trait>();
                foreach (Trait trait in traits)
                {
                    if (keys.Contains(trait.GetKey()))
                    {
                        filteredTraits.Add(trait);
                    }
                }

                return filteredTraits;
            }
            catch (JsonException e)
            {
                this.configuration.Logger?.LogError("\nJSON Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Get user trait for provided identity and trait key.
        /// </summary>
        public async Task<string> GetTrait(string identity, string key)
        {
            List<Trait> traits = await GetTraits(identity);
            if (traits == null)
            {
                return null;
            }

            foreach (Trait trait in traits)
            {
                if (trait.GetKey().Equals(key))
                {
                    return trait.GetStringValue();
                }
            }

            return null;
        }

        /// <summary>
        /// Get boolean user trait for provided identity and trait key.
        /// </summary>
        /// <returns>Null if Flagsmith is unaccessible</returns>
        public async Task<bool?> GetBoolTrait(string identity, string key)
        {
            List<Trait> traits = await GetTraits(identity);
            if (traits == null)
            {
                return null;
            }

            foreach (Trait trait in traits)
            {
                if (trait.GetKey().Equals(key))
                {
                    return trait.GetBoolValue();
                }
            }

            return false;
        }

        /// <summary>
        /// Get integer user trait for provided identity and trait key.
        /// </summary>
        /// <returns>Null if Flagsmith is unaccessible</returns>
        public async Task<int?> GetIntegerTrait(string identity, string key)
        {
            List<Trait> traits = await GetTraits(identity);
            if (traits == null)
            {
                return null;
            }

            foreach (Trait trait in traits)
            {
                if (trait.GetKey().Equals(key))
                {
                    return trait.GetIntValue();
                }
            }

            return 0;
        }

        /// <summary>
        /// Set user trait value for provided identity and trait key.
        /// </summary>
        public async Task<Trait> SetTrait(string identity, string key, object value)
        {
            try
            {
                if (!(value is bool) && !(value is int) && !(value is string))
                {
                    throw new ArgumentException("Value parameter must be string, int or boolean");
                }

                string url = configuration.ApiUrl.AppendPath("traits");
                string json = await GetJSON(HttpMethod.Post, url, JsonConvert.SerializeObject(new { identity = new { identifier = identity }, trait_key = key, trait_value = value }));

                return JsonConvert.DeserializeObject<Trait>(json);
            }
            catch (JsonException e)
            {
                this.configuration.Logger?.LogError("\nJSON Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                return null;
            }
            catch (ArgumentException e)
            {
                this.configuration.Logger?.LogError("\nArgument Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Increment user trait value for provided identity and trait key.
        /// </summary>
        public async Task<Trait> IncrementTrait(string identity, string key, int incrementBy)
        {
            try
            {
                string url = configuration.ApiUrl.AppendPath("traits", "increment-value");
                string json = await GetJSON(HttpMethod.Post, url,
                    JsonConvert.SerializeObject(new { identifier = identity, trait_key = key, increment_by = incrementBy }));

                return JsonConvert.DeserializeObject<Trait>(json);
            }
            catch (JsonException e)
            {
                this.configuration.Logger?.LogError("\nJSON Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Get both feature flags and user traits for the provided identity
        /// </summary>
        public async Task<Identity> GetUserIdentity(string identity)
        {
            try
            {
                string url = GetIdentitiesUrl(identity);
                string json = await GetJSON(HttpMethod.Get, url);

                return JsonConvert.DeserializeObject<Identity>(json);
            }
            catch (JsonException e)
            {
                this.configuration.Logger?.LogError("\nJSON Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                return null;
            }
        }

        protected virtual async Task<string> GetJSON(HttpMethod method, string url, string body = null)
        {
            try
            {
                var policy = HttpPolicies.GetRetryPolicyAwaitable(configuration.Retries);
                return await (await policy.ExecuteAsync(async () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, url)
                    {
                        Headers = {
                            { "X-Environment-Key", configuration.EnvironmentKey }
                        }
                    };
                    configuration.CustomHeaders?.ForEach(kvp => request.Headers.Add(kvp.Key, kvp.Value));
                    if (body != null)
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.RequestTimeout ?? 100));
                    HttpResponseMessage response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
                    return response.EnsureSuccessStatusCode();
                })).Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                this.configuration.Logger?.LogError("\nHTTP Request Exception Caught!");
                this.configuration.Logger?.LogError("Message :{0} ", e.Message);
                throw new FlagsmithAPIError("Unable to get valid response from Flagsmith API");
            }
            catch (TaskCanceledException)
            {
                throw new FlagsmithAPIError("Request cancelled: Api server takes too long to respond");
            }
        }

        private string GetIdentitiesUrl(string identity)
        {
            return configuration.ApiUrl.AppendPath("identities", identity);
        }
        protected async virtual Task GetAndUpdateEnvironmentFromApi()
        {
            try
            {
                var json = await GetJSON(HttpMethod.Get, configuration.ApiUrl + "environment-document/");
                Environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
                this.configuration.Logger?.LogInformation("Local Environment updated: " + json);
            }
            catch (FlagsmithAPIError ex)
            {
                this.configuration.Logger?.LogError(ex.Message);
            }
        }
        protected async virtual Task<List<Flag>> GetFeatureFlagsFromApi()
        {
            string url = configuration.ApiUrl.AppendPath("flags");
            string json = await GetJSON(HttpMethod.Get, url);
            var flags = JsonConvert.DeserializeObject<List<Flag>>(json);
            return new List<Flag>(AnalyticFlag.FromApiFlag(_AnalyticsProcessor, flags));
        }
        protected async virtual Task<List<Flag>> GetIdentityFlagsFromApi(string identity)
        {
            string url = GetIdentitiesUrl(identity);
            string json = await GetJSON(HttpMethod.Get, url);
            var flags = JsonConvert.DeserializeObject<Identity>(json)?.flags;
            return new List<Flag>(AnalyticFlag.FromApiFlag(_AnalyticsProcessor, flags));
        }
        protected virtual List<Flag> GetFeatureFlagsFromDocuments()
        {
            var analyticFlag = AnalyticFlag.FromFeatureStateModel(_AnalyticsProcessor, _Engine.GetEnvironmentFeatureStates(Environment));
            return new List<Flag>(analyticFlag);
        }
        protected virtual List<Flag> GetIdentityFlagsFromDocuments(string identifier, List<Trait> traits)
        {
            var identity = new IdentityModel { Identifier = identifier, IdentityTraits = traits?.Select(t => new TraitModel { TraitKey = t.GetKey(), TraitValue = t.GetIntValue() }).ToList() };
            var analyticFlag = AnalyticFlag.FromFeatureStateModel(_AnalyticsProcessor, _Engine.GetIdentityFeatureStates(Environment, identity), identity.CompositeKey);
            return new List<Flag>(analyticFlag);
        }
        ~FlagsmithClient() => _PollingManager.StopPoll();
    }

}
