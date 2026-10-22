using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flagsmith.Cache.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class HybridCacheTest
    {
        private const string Identifier = "identifier";

        private static HybridCache CreateHybridCache()
        {
            var services = new ServiceCollection();
            services.AddHybridCache();
            return services.BuildServiceProvider().GetRequiredService<HybridCache>();
        }

        private static Mock<HttpClient> MockIdentityResponse()
        {
            return HttpMocker.MockHttpResponse(HttpStatusCode.OK, Fixtures.ApiIdentityResponse, false);
        }

        private static FlagsmithClient CreateClient(
            Mock<HttpClient> mockHttpClient,
            Action<HybridCacheConfig> configure = null)
        {
            var hybridCacheConfig = new HybridCacheConfig(CreateHybridCache());
            configure?.Invoke(hybridCacheConfig);

            return new FlagsmithClient(new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                HybridCacheConfig = hybridCacheConfig
            });
        }

        private static FlagsmithClient CreateClientRefreshingOnTraitChanges(Mock<HttpClient> mockHttpClient)
        {
            return CreateClient(mockHttpClient, config => config.RefreshOnTraitChanges = true);
        }

        private static List<ITrait> Traits(params (string Key, string Value)[] traits)
        {
            var result = new List<ITrait>();
            foreach (var trait in traits)
            {
                result.Add(new Trait(trait.Key, trait.Value));
            }

            return result;
        }

        /// <summary>
        /// Cache entries are written by System.Text.Json on .NET 9 and later and by Newtonsoft.Json
        /// below that, and a shared L2 cache can hold entries written by either. Pinning the wire
        /// format here keeps the two readable by one another.
        /// </summary>
        [Fact]
        public void TestCacheEntryWireFormatIsPinned()
        {
            // Given
            var entry = new CachedFlagList
            {
                Flags =
                {
                    new CachedFlag
                    {
                        FeatureName = "some_feature",
                        FeatureId = 1,
                        Enabled = true,
                        Value = "some-value"
                    }
                },
                Traits = { ["foo"] = "bar" }
            };

            // When
            var json = CachedFlagListSerializer.Serialize(entry);

            // Then
            Assert.Equal(
                "{\"flags\":[{\"feature_name\":\"some_feature\",\"feature_id\":1,\"enabled\":true," +
                "\"value\":\"some-value\"}],\"traits\":{\"foo\":\"bar\"}}",
                json);
        }

        [Fact]
        public void TestCacheEntryRoundTripsThroughTheSerializer()
        {
            // Given
            var json =
                "{\"flags\":[{\"feature_name\":\"some_feature\",\"feature_id\":1,\"enabled\":true," +
                "\"value\":\"some-value\"}],\"traits\":{\"foo\":\"bar\"}}";

            // When
            var entry = CachedFlagListSerializer.Deserialize(json);

            // Then
            var flag = Assert.Single(entry.Flags);
            Assert.Equal("some_feature", flag.FeatureName);
            Assert.Equal(1, flag.FeatureId);
            Assert.True(flag.Enabled);
            Assert.Equal("some-value", flag.Value);
            Assert.Equal("bar", Assert.Single(entry.Traits).Value);
        }

        [Fact]
        public void TestCannotEnableBothCacheConfigAndHybridCacheConfig()
        {
            // Given
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                CacheConfig = new CacheConfig(true),
                HybridCacheConfig = new HybridCacheConfig(CreateHybridCache())
            };

            // Then
            var exception = Assert.Throws<Exception>(() => new FlagsmithClient(config));
            Assert.Equal("ValueError: Cannot use both cacheConfig and hybridCacheConfig.", exception.Message);
        }

        [Fact]
        public void TestCannotEnableHybridCacheWithoutACacheInstance()
        {
            // Given
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HybridCacheConfig = new HybridCacheConfig { Enabled = true }
            };

            // Then
            var exception = Assert.Throws<Exception>(() => new FlagsmithClient(config));
            Assert.Equal("ValueError: hybridCacheConfig.Cache must be provided to use HybridCache.", exception.Message);
        }

        [Fact]
        public async Task TestEnvironmentFlagsAreServedFromTheCache()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(HttpStatusCode.OK, Fixtures.ApiFlagResponse, false);
            var client = CreateClient(mockHttpClient);

            // When
            await client.GetEnvironmentFlags();
            var flags = (await client.GetEnvironmentFlags()).AllFlags();

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
        }

        [Fact]
        public async Task TestIdentityFlagsAreServedFromTheCacheWhenTraitsAreUnchanged()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClient(mockHttpClient);
            var traits = Traits(("foo", "bar"));

            // When
            await client.GetIdentityFlags(Identifier, traits);
            var flags = (await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")))).AllFlags();

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
        }

        [Fact]
        public async Task TestIdentityFlagsAreFetchedAgainWhenATraitIsAdded()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClientRefreshingOnTraitChanges(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar"), ("baz", "qux")));

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestIdentityFlagsAreFetchedAgainWhenATraitValueChanges()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClientRefreshingOnTraitChanges(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "changed")));

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestIdentityFlagsAreFetchedAgainWhenATraitBecomesTransient()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClientRefreshingOnTraitChanges(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, new List<ITrait> { new Trait("foo", "bar") });
            await client.GetIdentityFlags(Identifier, new List<ITrait> { new Trait("foo", "bar", true) });

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestIdentityFlagsStayCachedWhenATraitIsOmitted()
        {
            // Given: Flagsmith keeps traits it has already been told about, so dropping one from the
            // request cannot change the flags it returns.
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClientRefreshingOnTraitChanges(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar"), ("baz", "qux")));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await client.GetIdentityFlags(Identifier, new List<ITrait>());

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
        }

        [Fact]
        public async Task TestTraitsKnownBeforeARefreshAreStillKnownAfterIt()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClientRefreshingOnTraitChanges(mockHttpClient);

            // When: the second call adds a trait, so it refreshes. The third call carries only the
            // trait from the first one, which the refreshed entry must still remember.
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await client.GetIdentityFlags(Identifier, Traits(("baz", "qux")));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        /// <summary>
        /// HybridCache rejects a non-positive expiration when it writes an entry, which would
        /// otherwise surface as an exception from every flag read rather than at construction.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestCannotUseANonPositiveDuration(int minutes)
        {
            // Given
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HybridCacheConfig = new HybridCacheConfig(CreateHybridCache())
                {
                    Duration = TimeSpan.FromMinutes(minutes)
                }
            };

            // Then
            var exception = Assert.Throws<Exception>(() => new FlagsmithClient(config));
            Assert.Equal("ValueError: hybridCacheConfig.Duration must be positive.", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestCannotUseANonPositiveLocalCacheDuration(int minutes)
        {
            // Given
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HybridCacheConfig = new HybridCacheConfig(CreateHybridCache())
                {
                    LocalCacheDuration = TimeSpan.FromMinutes(minutes)
                }
            };

            // Then
            var exception = Assert.Throws<Exception>(() => new FlagsmithClient(config));
            Assert.Equal("ValueError: hybridCacheConfig.LocalCacheDuration must be positive.", exception.Message);
        }

        [Fact]
        public void TestAnUnsetLocalCacheDurationIsAccepted()
        {
            var mockHttpClient = MockIdentityResponse();

            // Then: no throw
            _ = CreateClient(mockHttpClient, config => config.LocalCacheDuration = null);
        }

        [Fact]
        public void TestRefreshOnTraitChangesIsOffByDefault()
        {
            Assert.False(new HybridCacheConfig().RefreshOnTraitChanges);
            Assert.False(new HybridCacheConfig(CreateHybridCache()).RefreshOnTraitChanges);
        }

        /// <summary>
        /// The default keeps evaluation stateless, matching the other server-side SDKs: a trait change
        /// waits out the cache duration rather than invalidating the entry.
        /// </summary>
        [Fact]
        public async Task TestIdentityFlagsStayCachedOnTraitChangesByDefault()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClient(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "changed"), ("baz", "qux")));

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
        }

        [Fact]
        public async Task TestIdentityFlagsAreFetchedAgainOnceTheCacheEntryExpires()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClient(mockHttpClient, config =>
            {
                config.Duration = TimeSpan.FromMilliseconds(100);
                config.RefreshOnTraitChanges = false;
            });

            // When
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));
            await Task.Delay(TimeSpan.FromMilliseconds(300));
            await client.GetIdentityFlags(Identifier, Traits(("foo", "bar")));

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestIdentitiesAreCachedSeparately()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClient(mockHttpClient);

            // When
            await client.GetIdentityFlags("first");
            await client.GetIdentityFlags("second");
            await client.GetIdentityFlags("first");

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestTransientIdentitiesAreCachedSeparatelyFromPersistedOnes()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var client = CreateClient(mockHttpClient);

            // When
            await client.GetIdentityFlags(Identifier, null);
            await client.GetIdentityFlags(Identifier, null, true);

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestClientsWithDifferentEnvironmentKeysDoNotShareCacheEntries()
        {
            // Given
            var mockHttpClient = MockIdentityResponse();
            var sharedCache = CreateHybridCache();

            FlagsmithClient ClientFor(string environmentKey) => new FlagsmithClient(new FlagsmithConfiguration
            {
                EnvironmentKey = environmentKey,
                HttpClient = mockHttpClient.Object,
                HybridCacheConfig = new HybridCacheConfig(sharedCache)
            });

            // When
            await ClientFor("first-environment-key").GetIdentityFlags(Identifier);
            await ClientFor("second-environment-key").GetIdentityFlags(Identifier);

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", () => Times.Exactly(2));
        }
    }
}
