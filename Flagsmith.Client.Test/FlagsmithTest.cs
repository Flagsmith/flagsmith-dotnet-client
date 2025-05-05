using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FlagsmithEngine.Environment.Models;
using Moq;
using Newtonsoft.Json.Linq;
using OfflineHandler;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        [Fact]
        public void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            _ = new FlagsmithClient(config);
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async void TestUpdateEnvironmentSetsEnvironment()
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
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClientTest = new FlagsmithClient(config);

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Never);
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
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
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClientTest = new FlagsmithClient(config);

            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.Equal(fs.Enabled, flags[0].Enabled);
            Assert.Equal(fs.GetValue(), flags[0].Value);
            Assert.Equal(fs.Feature.Name, flags[0].GetFeatureName());
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async Task TestThatCacheDictionaryDoesNotThrowUnderLoad()
        {
            const int numberOfThreads = 500;

            ThreadPool.SetMinThreads(numberOfThreads, numberOfThreads);

            var mockHttpClient = HttpMocker.MockHttpResponse(HttpStatusCode.OK, Fixtures.ApiIdentityResponse, false);

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                CacheConfig = new CacheConfig(true)
            };
            var flagsmithClientTest = new FlagsmithClient(config);

            var token = new CancellationToken();
            await Parallel.ForEachAsync(Enumerable.Range(1, numberOfThreads), token, async (item, _) =>
            {
                (await flagsmithClientTest.GetIdentityFlags(item.ToString())).AllFlags();
            });
        }
        [Theory]
        [InlineData("identifier", "{\"identifier\":\"identifier\",\"traits\":[],\"transient\":false}")]
        [InlineData("identifier&h=g", "{\"identifier\":\"identifier&h=g\",\"traits\":[],\"transient\":false}")]
        public async Task TestGetIdentityFlagsCallsPostApiWhenNoLocalEnvironmentNoTraits(string identifier, string expectedJson)
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            var flags = (await flagsmithClientTest.GetIdentityFlags(identifier)).AllFlags();
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once, expectedJson);
        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsPostApiWhenNoLocalEnvironmentWithTraits()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            var traits = new List<ITrait> { new Trait("foo", "bar") };


            var flags = (await flagsmithClientTest.GetIdentityFlags("identifier", traits)).AllFlags();
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);

        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);

            _ = await flagsmithClientTest.GetIdentityFlags("identifier", new List<ITrait>() { new Trait("foo", "bar") });

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalIdentityOverridesWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });

            var traits = new List<ITrait> { new Trait("foo", "bar") };

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);

            var flags = await flagsmithClientTest.GetIdentityFlags("overridden-id", traits);
            var flag = await flags.GetFlag("some_feature");
            Assert.False(flag.Enabled);
            Assert.Equal("some-overridden-value", flag.Value);
        }
        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                DefaultFlagHandler = _ => defaultFlag
            };
            var flagsmithClientTest = new FlagsmithClient(config);
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                DefaultFlagHandler = _ => defaultFlag
            };
            var flagsmithClientTest = new FlagsmithClient(config);
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                DefaultFlagHandler = _ => defaultFlag
            };
            var flagsmithClientTest = new FlagsmithClient(config);
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                DefaultFlagHandler = _ => defaultFlag
            };
            var flagsmithClientTest = new FlagsmithClient(config);
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
                StatusCode = HttpStatusCode.Forbidden,
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                DefaultFlagHandler = _ => defaultFlag
            };
            var flagsmithClientTest = new FlagsmithClient(config);
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
                StatusCode = HttpStatusCode.OK
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClient = new FlagsmithClient(config);

            await flagsmithClient.GetIdentityFlags(identifier, traits);

            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            // TODO: verify the body is correct - I've verified manually but can't verify programmatically
        }

        [Fact]
        public void TestGetIdentitySegmentsNoTraits()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClient = new FlagsmithClient(config);

            // When
            var segments = flagsmithClient.GetIdentitySegments("identifier");

            // Then
            Assert.Empty(segments);
        }

        [Fact]
        public void TestGetIdentitySegmentsWithValidTrait()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true
            };
            var flagsmithClient = new FlagsmithClient(config);

            var identifier = "identifier";
            var traits = new List<ITrait>() { new Trait(traitKey: "foo", traitValue: "bar") };

            // When
            var segments = flagsmithClient.GetIdentitySegments(identifier, traits);

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
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableLocalEvaluation = true,
                CacheConfig = new CacheConfig(true)
            };
            var flagsmithClient = new FlagsmithClient(config);

            // When
            var flags = flagsmithClient.GetEnvironmentFlags().Result;

            // Then
            Assert.NotNull(flags);
        }


        [Fact]
        public async Task TestOfflineMode_IntegrationTest()
        {
            // Given
            JObject
                .Parse(File.ReadAllText("../../../data/offline-environment.json"))
                .ToObject<EnvironmentModel>();

            const string expectedPath = "../../../data/offline-environment.json";

            var localFileHandler = new LocalFileHandler(expectedPath);

            // When
            var config = new FlagsmithConfiguration
            {
                OfflineMode = true,
                OfflineHandler = localFileHandler
            };
            var flagsmithClient = new FlagsmithClient(config);

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
                .Parse(await File.ReadAllTextAsync("../../../data/offline-environment.json"))
                .ToObject<EnvironmentModel>();

            const string apiUrl = "http://some.flagsmith.com/api/v1/";
            var mockOfflineHandler = new Mock<BaseOfflineHandler>();
            var mockFlagsmithClient = new Mock<IFlagsmithClient>();

            mockOfflineHandler.Setup(h => h.GetEnvironment()).Returns(environment);

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = "some-key",
                HttpClient = mockHttpClient.Object,
                ApiUri = new Uri(apiUrl),
                OfflineHandler = mockOfflineHandler.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);

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
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                OfflineHandler = mockOfflineHandler.Object
            };
            var flagsmithClientTest = new FlagsmithClient(config);

            // Then
            var environmentFlags = await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
            Assert.True(await environmentFlags.IsFeatureEnabled("some_feature"));
            Assert.NotEqual("offline-value", await environmentFlags.GetFeatureValue("some_feature"));
            Assert.Equal("some-value", await environmentFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public void TestCannotUseOfflineModeWithoutOfflineHandler()
        {
            // When
            void CreateFlagsmith()
            {
                var config = new FlagsmithConfiguration { OfflineMode = true, OfflineHandler = null };
                _ = new FlagsmithClient(config);
            }

            // Then
            var exception = Assert.Throws<Exception>(CreateFlagsmith);
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
            void CreateFlagsmith()
            {
                var config = new FlagsmithConfiguration { OfflineHandler = localFileHandler, DefaultFlagHandler = _ => defaultFlag };
                _ = new FlagsmithClient(config);
            }

            // Then
            var exception = Assert.Throws<Exception>(CreateFlagsmith);
            Assert.Equal("ValueError: Cannot use both defaultFlagHandler and offlineHandler.", exception.Message);
        }

        [Fact]
        public void TestCannotCreateFlagsmithClientInRemoteEvaluationWithoutAPIKey()
        {
            // When
            void CreateFlagsmith() => _ = new FlagsmithClient(new FlagsmithConfiguration());

            // Then
            var exception = Assert.Throws<Exception>(CreateFlagsmith);
            Assert.Equal("ValueError: environmentKey is required", exception.Message);
        }

        [Fact]
        public void TestCannotCreateFlagsmithClientInLocalEvaluationWithoutServerAPIKey()
        {
            // When
            Action createFlagsmith = () => new FlagsmithClient(
                new FlagsmithConfiguration
                {
                    EnvironmentKey = "foobar",
                    EnableLocalEvaluation = true
                }
            );

            // Then
            var exception = Assert.Throws<Exception>(() => createFlagsmith());
            Assert.Equal
            (
                "ValueError: In order to use local evaluation, please generate a server key in the environment settings page.",
                exception.Message
            );
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponseWithTenFlags)
            });
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                EnableAnalytics = true
            };
            var flagsmithClientTest = new FlagsmithClient(config);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var featuresDictionary = new Dictionary<string, int>();

            const int numberOfFeatures = 10;
            const int numberOfThreads = 1000;
            const int callsPerThread = 1000;

            ThreadPool.SetMinThreads(numberOfThreads, numberOfThreads);

            for (int i = 1; i <= numberOfFeatures; i++)
            {
                featuresDictionary.TryAdd($"Feature_{i}", 0);
            }

            // When
            var tasks = new Task[numberOfThreads];

            // Create numberOfThreads threads.
            for (var i = 0; i < numberOfThreads; i++)
            {
                string[] features = new string[callsPerThread];

                // Prepare an array of feature names of length callsPerThread.
                for (var j = 0; j < callsPerThread; j++)
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
            var analyticsData = flagsmithClientTest.aggregatedAnalytics;
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
            // Given
            string identifier = "transient_identity";
            var traits = new List<ITrait> { new Trait("some_trait", "some_value") };
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiTransientIdentityResponse)
            });
            // When
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClient = new FlagsmithClient(config);
            var identityFlags = await flagsmithClient.GetIdentityFlags(identifier, traits, true);
            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once, "{\"identifier\":\"transient_identity\",\"traits\":[{\"trait_key\":\"some_trait\",\"trait_value\":\"some_value\",\"transient\":false}],\"transient\":true}");
            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("some-identity-trait-value", await identityFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public async Task TestGetIdentityFlagsTransientTraitKeysCallsExpected()
        {
            // Given
            string identifier = "test_identity_with_transient_traits";
            var traits = new List<ITrait> { new Trait("transient_trait", "transient_trait_value", true) };

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityWithTransientTraitsResponse)
            });
            // When
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClient = new FlagsmithClient(config);
            var identityFlags = await flagsmithClient.GetIdentityFlags(identifier, traits);
            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once, "{\"identifier\":\"test_identity_with_transient_traits\",\"traits\":[{\"trait_key\":\"transient_trait\",\"trait_value\":\"transient_trait_value\",\"transient\":true}],\"transient\":false}");
            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("some-transient-trait-value", await identityFlags.GetFeatureValue("some_feature"));
        }
        [Fact]
        public async Task TestGetIdentityFlagsTransientIdentityWithoutTraitCallsExpected()
        {
            // Given
            string identifier = "transient_identity";
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiTransientIdentityResponse)
            });
            // When
            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object
            };
            var flagsmithClient = new FlagsmithClient(config);
            var identityFlags = await flagsmithClient.GetIdentityFlags(identifier, null, true);
            // Then
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once, "{\"identifier\":\"transient_identity\",\"traits\":[],\"transient\":true}");
            Assert.True(await identityFlags.IsFeatureEnabled("some_feature"));
            Assert.Equal("some-identity-trait-value", await identityFlags.GetFeatureValue("some_feature"));
        }

        [Fact]
        public void TestRequestTimeoutInterpretsSecondsCorrectly()
        {
            var config = new FlagsmithConfiguration
            {
                RequestTimeout = 100
            };
            Assert.Equal(100, config.RequestTimeout);
        }

        [Fact]
        public void TestRequestTimeoutHasReasonableDefault()
        {
            Assert.True(new FlagsmithConfiguration().RequestTimeout > 1);
        }
    }
}
