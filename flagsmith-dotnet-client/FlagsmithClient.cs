using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class FlagsmithClient
    {
        public static FlagsmithClient instance;

        private readonly FlagsmithConfiguration configuration;
        private static HttpClient httpClient;

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
                var sp = ServicePointManager.FindServicePoint(new Uri(configuration.ApiUrl));
                sp.ConnectionLeaseTimeout = 60 * 1000 * 5;
                httpClient = new HttpClient();
                instance = this;
            }
            else
            {
                throw new NotSupportedException("FlagsmithClient should only be initialised once. Use FlagsmithClient.instance after successful initialisation");
            }
        }

        /// <summary>
        /// Get all feature flags (flags and remote config) optionally for a specific identity.
        /// </summary>
        public async Task<List<Flag>> GetFeatureFlags(string identity = null)
        {
            string url;
            if (identity == null)
            {
                url = configuration.ApiUrl.AppendPath("flags");
            }
            else
            {
                url = GetIdentitiesUrl(identity);
            }

            try
            {
                string json = await GetJSON(HttpMethod.Get, url);

                if (identity == null)
                {
                    return JsonConvert.DeserializeObject<List<Flag>>(json);
                }
                else
                {
                    return JsonConvert.DeserializeObject<Identity>(json)?.flags;
                }
            }
            catch (JsonException e)
            {
                Console.WriteLine("\nJSON Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Check feature exists and is enabled optionally for a specific identity
        /// </summary>
        /// <returns>Null if Flagsmith is unaccessible</returns>
        public async Task<bool?> HasFeatureFlag(string featureId, string identity = null)
        {
            List<Flag> flags = await GetFeatureFlags(identity);
            if (flags == null)
            {
                return null;
            }

            foreach (Flag flag in flags)
            {
                if (flag.GetFeature().GetName().Equals(featureId) && flag.IsEnabled())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get remote config value optionally for a specific identity
        /// </summary>
        public async Task<string> GetFeatureValue(string featureId, string identity = null)
        {
            List<Flag> flags = await GetFeatureFlags(identity);
            if (flags == null)
            {
                return null;
            }

            foreach (Flag flag in flags)
            {
                if (flag.GetFeature().GetName().Equals(featureId))
                {
                    return flag.GetValue();
                }
            }

            return null;
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
                Console.WriteLine("\nJSON Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
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
                Console.WriteLine("\nJSON Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("\nArgument Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
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
                Console.WriteLine("\nJSON Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
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
                Console.WriteLine("\nJSON Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        private async Task<string> GetJSON(HttpMethod method, string url, string body = null)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(method, url)
                {
                    Headers = {
                        { "X-Environment-Key", configuration.EnvironmentKey }
                    }
                };
                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                HttpResponseMessage response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nHTTP Request Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return string.Empty;
            }
        }

        private string GetIdentitiesUrl(string identity)
        {
            if (configuration.UseLegacyIdentities)
            {
                return configuration.ApiUrl.AppendPath("identities", identity);
            }

            return configuration.ApiUrl.AppendToUrl(trailingSlash: false, "identities", $"?identifier={identity}");
        }
    }
}
