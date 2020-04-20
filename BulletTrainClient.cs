using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace SolidStateGroup.BulletTrain
{
    public class BulletTrainClient
    {
        private readonly BulletTrainConfiguration configuration;

        private static HttpClient httpClient;
        private static bool isInitialized;

        public BulletTrainClient(BulletTrainConfiguration bulletTrainConfiguration)
        {
            if (bulletTrainConfiguration == null)
            {
                throw new ArgumentNullException(nameof(bulletTrainConfiguration),
                    "Parameter must be provided when constructing an instance of the client.");
            }

            if (!bulletTrainConfiguration.IsValid())
            {
                throw new ArgumentException("The provided configuration is not valid. An API Url and Environment Key must be provided.", nameof(bulletTrainConfiguration));
            }

            if (!isInitialized)
            {
                configuration = bulletTrainConfiguration;
                var sp = ServicePointManager.FindServicePoint(new Uri(configuration.ApiUrl));
                sp.ConnectionLeaseTimeout = 60 * 1000 * 5;
                httpClient = new HttpClient();
                isInitialized = true;
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
                url = configuration.ApiUrl + "flags/";
            }
            else
            {
                url = configuration.ApiUrl + "identities/" + identity + "/";
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
                    return JsonConvert.DeserializeObject<Identity>(json).flags;
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
        public async Task<bool> HasFeatureFlag(string featureId, string identity = null)
        {
            List<Flag> flags = await GetFeatureFlags(identity);
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
                string json = await GetJSON(HttpMethod.Get, configuration.ApiUrl + "identities/" + identity + "/");

                List<Trait> traits = JsonConvert.DeserializeObject<Identity>(json).traits;
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

            foreach (Trait trait in traits)
            {
                if (trait.GetKey().Equals(key))
                {
                    return trait.GetValue();
                }
            }

            return null;
        }

        /// <summary>
        /// Set user trait value for provided identity and trait key.
        /// </summary>
        public async Task<Trait> SetTrait(string identity, string key, string value)
        {
            try
            {
                string json = await GetJSON(HttpMethod.Post, configuration.ApiUrl + "identities/" + identity + "/traits/" + key, JsonConvert.SerializeObject(new { trait_value = value }));

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
                string json = await GetJSON(HttpMethod.Get, configuration.ApiUrl + "identities/" + identity + "/");

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
                return null;
            }
        }
    }
}
