using Moq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FlagsmithEngine.Environment.Models;
using OfflineHandler;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Net;

namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        [Fact]
        public void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, enableClientSideEvaluation: true);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async void TestUpdateEnvironmentSetsEnvironment()
        {
            // Given
            var responses = new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/environment-document/", new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.JsonObject.ToString())
                    }
                },
                { "/api/v1/flags/", new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.ApiFlagResponse)
                    }
                }
            };
            var mockHttpClient = HttpMocker.MockHttpResponse(responses);

            // When
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);

            // Then
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Never);
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            // Given
            var responses = new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/environment-document/", new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.JsonObject.ToString())
                    }
                },
                { "/api/v1/flags/", new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.ApiFlagResponse)
                    }
                }
            };
            var mockHttpClient = HttpMocker.MockHttpResponse(responses);

            // When
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);

            // Then
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.Equal(fs.Enabled, flags[0].Enabled);
            Assert.Equal(fs.GetValue(), flags[0].Value);
            Assert.Equal(fs.Feature.Name, flags[0].GetFeatureName());
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsGetApiWhenNoLocalEnvironmentNoTraits()
        {
            string identifier = "identifier";
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetIdentityFlags(identifier)).AllFlags();
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/identities/", Times.Once, new Dictionary<string, string> { { "identifier", identifier } });

        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsPostApiWhenNoLocalEnvironmentWithTraits()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var traits = new List<ITrait> { new Trait("foo", "bar") };


            var flags = (await flagsmithClientTest.GetIdentityFlags("identifier", traits)).AllFlags();
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);

        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });

            var traits = new List<ITrait> { new Trait("foo", "bar") };

            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);

            _ = await flagsmithClientTest.GetIdentityFlags("identifier", new List<ITrait>() { new Trait("foo", "bar") });

            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalIdentityOverridesWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });

            var traits = new List<ITrait> { new Trait("foo", "bar") };

            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);

            var flags = await flagsmithClientTest.GetIdentityFlags("overridden-id", traits);
            var flag = await flags.GetFlag("some_feature");
            Assert.False(flag.Enabled);
            Assert.Equal("some-overridden-value", flag.Value);
        }
        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestGetIdentityFlagsSendsTraits()
        {
            string identifier = "identifier";
            var traits = new List<ITrait>() { new Trait("foo", "bar"), new Trait("ifoo", 1) };

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });
            var flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);

            var flags = await flagsmithClient.GetIdentityFlags(identifier, traits);

            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            // TODO: verify the body is correct - I've verified manually but can't verify programmatically
        }

        [Fact]
        public void TestGetIdentitySegmentsNoTraits()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            FlagsmithClient flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, enableClientSideEvaluation: true);

            // When
            List<ISegment> segments = flagsmithClient.GetIdentitySegments("identifier");

            // Then
            Assert.Empty(segments);
        }

        [Fact]
        public void TestGetIdentitySegmentsWithValidTrait()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            FlagsmithClient flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, enableClientSideEvaluation: true);

            string identifier = "identifier";
            List<ITrait> traits = new List<ITrait>() { new Trait(traitKey: "foo", traitValue: "bar") };

            // When
            List<ISegment> segments = flagsmithClient.GetIdentitySegments(identifier, traits);

            // Then
            Assert.Single(segments);
            Assert.Equal("Test segment", segments[0].Name);
        }

        [Fact]
        public void TestFlagsmithClientWithCacheInitialization()
        {
            // Given
            var responses = new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/environment-document/", new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.JsonObject.ToString())
                    }
                },
                { "/api/v1/flags/", new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(Fixtures.ApiFlagResponse)
                    }
                }
            };
            var mockHttpClient = HttpMocker.MockHttpResponse(responses);
            var flagsmithClient = new FlagsmithClient(Fixtures.ApiKey,
                httpClient: mockHttpClient.Object,
                enableClientSideEvaluation: true,
                cacheConfig: new CacheConfig(true));

            // When
            var flags = flagsmithClient.GetEnvironmentFlags().Result;

            // Then
            Assert.NotNull(flags);
        }


        [Fact]
        public async Task TestOfflineMode_IntegrationTest()
        {
            // Given
            var environment = JObject
                .Parse(File.ReadAllText("../../../data/offline-environment.json"))
                .ToObject<EnvironmentModel>();

            var expectedPath = "../../../data/offline-environment.json";

            var localFileHandler = new LocalFileHandler(expectedPath);

            // When
            var flagsmithClient = new FlagsmithClient(
                offlineMode: true,
                offlineHandler: localFileHandler
            );

            // Then
            // we can request the flags from the client successfully
            var environmentFlags = await flagsmithClient.GetEnvironmentFlags();
            var flag = await environmentFlags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("offline-value", flag.Value);

            var identityFlags = await flagsmithClient.GetIdentityFlags("identity");
            flag = await identityFlags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("offline-value", flag.Value);
        }

        [Fact]
        public async Task TestFlagsmithUsesOfflineHandlerIfSetAndNoAPIResponse()
        {
            // Given
            var environment = JObject
                .Parse(File.ReadAllText("../../../data/offline-environment.json"))
                .ToObject<EnvironmentModel>();

            var apiUrl = "http://some.flagsmith.com/api/v1/";
            var mockOfflineHandler = new Mock<BaseOfflineHandler>();
            var mockFlagsmithClient = new Mock<IFlagsmithClient>();

            mockOfflineHandler.Setup(h => h.GetEnvironment()).Returns(environment);

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

            var flagsmithClientTest = new FlagsmithClient(
                environmentKey: "some-key",
                httpClient: mockHttpClient.Object,
                apiUrl: apiUrl,
                offlineHandler: mockOfflineHandler.Object
                );

            // When
            mockFlagsmithClient.Setup(f => f.GetEnvironmentFlags()).ReturnsAsync(await flagsmithClientTest.GetEnvironmentFlags());
            mockFlagsmithClient.Setup(f => f.GetIdentityFlags("identity")).ReturnsAsync(await flagsmithClientTest.GetIdentityFlags("identity"));

            // Then
            mockOfflineHandler.Verify(h => h.GetEnvironment(), Times.AtMost(2));
            mockFlagsmithClient.Verify(h => h.GetEnvironmentFlags(), Times.AtMostOnce());
            mockFlagsmithClient.Verify(h => h.GetIdentityFlags("identity"), Times.AtMostOnce());

            var environmentFlags = await flagsmithClientTest.GetEnvironmentFlags();
            var identityFlags = await flagsmithClientTest.GetIdentityFlags("identity");

            Assert.True(await environmentFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("offline-value", await environmentFlags.GetFeatureValue("some_feature"));

            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("offline-value", await identityFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public async Task TestFlagsmithUsesTheAPIResponseEvenIfTheOfflineHandlerIsSet()
        {
            // Given
            var environment = JObject
                .Parse(File.ReadAllText("../../../data/offline-environment.json"))
                .ToObject<EnvironmentModel>();

            var mockOfflineHandler = new Mock<BaseOfflineHandler>();
            mockOfflineHandler.Setup(h => h.GetEnvironment()).Returns(environment);
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse),
            });

            // When
            FlagsmithClient flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, offlineHandler: mockOfflineHandler.Object);

            // Then
            var environmentFlags = await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
            Assert.True(await environmentFlags.IsFeatureEnabled("some_feature"));
            Assert.NotEqual("offline-value", await environmentFlags.GetFeatureValue("some_feature"));
            Assert.Equal("some-value", await environmentFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public void TestCannotUseOfflineModeWithoutOfflineHandler()
        {
            // When
            Action createFlagsmith = () => new FlagsmithClient(offlineMode: true, offlineHandler: null);

            // Then
            var exception = Assert.Throws<Exception>(() => createFlagsmith());
            Assert.Equal("ValueError: offlineHandler must be provided to use offline mode.", exception.Message);
        }

        [Fact]
        public void TestCannotUseBothDefaultHandlerAndOfflineHandler()
        {
            // Given
            var defaultFlag = new Flag(null, true, "some-default-value");
            var expectedPath = "../../../data/offline-environment.json";
            var localFileHandler = new LocalFileHandler(expectedPath);

            // When
            Action createFlagsmith = () => new FlagsmithClient(
                    offlineHandler: localFileHandler,
                    defaultFlagHandler: (string name) => defaultFlag
                );

            // Then
            var exception = Assert.Throws<Exception>(() => createFlagsmith());
            Assert.Equal("ValueError: Cannot use both defaultFlagHandler and offlineHandler.", exception.Message);
        }

        [Fact]
        public void TestCannotCreateFlagsmithClientInRemoteEvaluationWithoutAPIKey()
        {
            // When
            Action createFlagsmith = () => new FlagsmithClient();

            // Then
            var exception = Assert.Throws<Exception>(() => createFlagsmith());
            Assert.Equal("ValueError: environmentKey is required", exception.Message);
        }

        [Fact]
        /// <summary>
        /// Test that analytics data is consistent with concurrent calls to get flags.
        /// A huge number of threads are spawned to ensure that the issues related with
        /// concurrency are systematically reproduced even in machines with good resources.
        /// Tested with a MacBook Pro M2 with 16GB of RAM and a Core i7 PC with 48 GB of RAM.
        /// The increment on execution time in the CI runners is not significant.
        /// </summary>
        public async Task TestAnalyticsDataConsistencyWithConcurrentCallsToGetFlags()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponseWithTenFlags)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, enableAnalytics: true);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();

            Dictionary<string, int> featuresDictionary = new Dictionary<string, int>();

            const int numberOfFeatures = 10;
            const int numberOfThreads = 1000;
            const int callsPerThread = 1000;

            for (int i = 1; i <= numberOfFeatures; i++)
            {
                featuresDictionary.TryAdd($"Feature_{i}", 0);
            }

            // When 
            var tasks = new Task[numberOfThreads];

            // Create numberOfThreads threads.
            for (int i = 0; i < numberOfThreads; i++)
            {
                var local = i;
                string[] features = new string[callsPerThread];

                // Prepare an array of feature names of length callsPerThread.
                for (int j = 0; j < callsPerThread; j++)
                {
                    // The feature names are randomly selected from the featuresDictionary and added to the 
                    // list of features, which represents the features that have been evaluated.  
                    string featureName = $"Feature_{new Random().Next(1, featuresDictionary.Count + 1)}";
                    features[j] = featureName;

                    // The relevant key in the featuresDictionary is incremented to simulate an evaluation
                    // to track for that feature. 
                    featuresDictionary[featureName]++;
                }

                // Each thread will call IsFeatureEnabled for callsPerThread times.
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var feature in features)
                    {
                        await flags.IsFeatureEnabled(feature);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Then
            Dictionary<string, int> analyticsData = flagsmithClientTest.aggregatedAnalytics;
            int totalCallsMade = 0;
            foreach (var feature in featuresDictionary)
            {
                totalCallsMade += analyticsData[feature.Key];
                Assert.Equal(feature.Value, analyticsData[feature.Key]);
            }
            Assert.Equal(numberOfThreads * callsPerThread, totalCallsMade);
        }

        [Fact]
        public async Task TestGetIdentityFlagsTransientIdentityCallsExpected()
        {
            string identifier = "transient_identity";
            bool transient = true;
            var traits = new List<ITrait> { new Trait("some_trait", "some_value") };
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiTransientIdentityResponse)
            });
            var flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var identityFlags = await flagsmithClient.GetIdentityFlags(identifier, traits, transient);
            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("some-identity-trait-value", await identityFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public async Task TestGetIdentityFlagsTransientTraitKeysCallsExpected()
        {
            string identifier = "test_identity_with_transient_traits";
            var traits = new List<ITrait> { new Trait("transient_trait", "transient_trait_value", true) };

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityWithTransientTraitsResponse)
            });
            var flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var identityFlags = await flagsmithClient.GetIdentityFlags(identifier, traits);
            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("some-transient-trait-value", await identityFlags.GetFeatureValue("some_feature"));
        }
    }
}
