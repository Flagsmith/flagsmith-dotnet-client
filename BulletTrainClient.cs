using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BulletTrain
{
    public class BulletTrainClient : IBulletTrainClient
    {
        private readonly BulletTrainConfiguration _configuration;
        private readonly IBulletTrainHttpClient _bulletTrainHttpClient;

        public BulletTrainClient(BulletTrainConfiguration configuration, IBulletTrainHttpClient bulletTrainHttpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration),
                "Parameter must be provided when constructing an instance of the client.");
            _bulletTrainHttpClient = bulletTrainHttpClient ?? throw new ArgumentNullException(nameof(configuration),
                "Parameter must be provided when constructing an instance of the client.");

            if (!_configuration.IsValid())
            {
                throw new ArgumentException("The provided configuration is not valid. An API Url and Environment Key must be provided.", nameof(configuration));
            }
        }

        public async Task<List<Flag>> GetFeatureFlags(string identity = null)
        {
            if (!string.IsNullOrWhiteSpace(identity))
            {
                var userIdentity = await GetUserIdentityAsync(identity);

                return userIdentity.Flags;
            }

            var uri = GetUri("flags");

            return await _bulletTrainHttpClient.GetAsync<List<Flag>>(uri);
        }

        public async Task<bool> HasFeatureFlag(string featureId, string identity = null)
        {
            var flags =  await GetFeatureFlags(identity);

            return flags.Any(f => f.IsEnabled() && f.GetFeature().GetName().Equals(featureId));
        }

        public async Task<string> GetFeatureValue(string featureId, string identity = null)
        {
            var flags =  await GetFeatureFlags(identity);

            return flags.SingleOrDefault(f => f.GetFeature().GetName().Equals(featureId))?.GetValue();
        }

        public async Task<List<Trait>> GetTraits(string identity, List<string> keys = null)
        {
            var userIdentity = await GetUserIdentityAsync(identity);

            if (keys == null)
            {
                return userIdentity.Traits;
            }

            var keysSet = new HashSet<string>(keys);
            var filteredTraits = userIdentity.Traits.Where(
                t => keysSet.Contains(t.GetKey())
            );

            return filteredTraits.ToList();
        }

        public async Task<string> GetTrait(string identity, string key)
        {
            var traits = await GetTraits(identity);

            return traits.SingleOrDefault(t => t.GetKey().Equals(key))?.GetStringValue();
        }

        public async Task<bool> GetBoolTrait(string identity, string key)
        {
            var traits = await GetTraits(identity);

            return traits.SingleOrDefault(t => t.GetKey().Equals(key))?.GetBoolValue() ?? false;
        }

        public async Task<int> GetIntegerTrait(string identity, string key)
        {
            var traits = await GetTraits(identity);

            return traits.SingleOrDefault(t => t.GetKey().Equals(key))?.GetIntValue() ?? 0;
        }

        public Task<Trait> SetTrait(string identity, string key, object value)
        {
            if (!(value is bool) && !(value is int) && !(value is string))
            {
                throw new ArgumentException("Value parameter must be string, int or boolean");
            }

            return _bulletTrainHttpClient.PostAsync<Trait>(GetUri("traits"), new
            {
                identity = new
                {
                    identifier = identity
                },
                trait_key = key,
                trait_value = value,
            });
        }

        public Task<Trait> IncrementTrait(string identity, string key, int incrementBy)
        {
            return _bulletTrainHttpClient.PostAsync<Trait>(GetUri("traits/increment-value/"), new
            {
                identifier = identity,
                trait_key = key, 
                increment_by = incrementBy,
            });
        }

        public Task<Identity> GetUserIdentity(string identity)
        {
            return GetUserIdentityAsync(identity);
        }

        private async Task<Identity> GetUserIdentityAsync(string identity)
        {
            var uri = GetUri("identities", identity);

            try
            {
                return await _bulletTrainHttpClient.GetAsync<Identity>(uri);
            }
            catch (JsonException e)
            {
                
                return null;
            }
        }

        private Uri GetUri(params string[] pathSegments)
        {
            var path = _configuration.ApiUrl.AppendPath(pathSegments);
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return uri;
            }

            throw new ArgumentException($"Cannot create service uri {path}.");
        }
    }
}