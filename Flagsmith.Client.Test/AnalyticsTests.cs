using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class AnalyticsTests
    {
        static readonly Uri _defaultApiUri = new Uri("https://edge.api.flagsmith.com/api/v1/");
        static readonly Uri _defaultAnalyticsUri = new Uri(_defaultApiUri, "analytics/flags/");

        [Fact]
        public async Task TestAnalyticsProcessorDoesNotThrowUnderLoad()
        {
            const int numberOfThreads = 500;
            const int numberOfTracksPerThread = 7;
            const string key = "TestAnalyticsProcessorFlushClearsAnalyticsDataFeature";

            ThreadPool.SetMinThreads(numberOfThreads, numberOfThreads);
            HttpMocker.PayloadsSubmitted = new System.Collections.Concurrent.ConcurrentBag<string>();

            var mockHttpClient = HttpMocker.MockHttpResponse(System.Net.HttpStatusCode.OK, null, true);
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, _defaultAnalyticsUri);
            var token = new CancellationToken();
            await Parallel.ForEachAsync(Enumerable.Range(1, numberOfThreads), token, async (item, token) =>
            {
                for (int i = 0; i < numberOfTracksPerThread; i++)
                {
                    await analyticsProcessor.TrackFeature(key);
                }
                await analyticsProcessor.Flush();
            });

            var totalTrackedFeatureCount = HttpMocker.PayloadsSubmitted.Select(z =>
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, int>>(z);

                return data.TryGetValue(key, out var count) ? count : 0;
            }).Sum();

            // Assert that the volume of entries flushed was as expected
            Assert.Equal(numberOfTracksPerThread * numberOfThreads, totalTrackedFeatureCount);

            await Task.Delay(11000);
            // Make sure that Flush on track doesn't lock up
            await analyticsProcessor.TrackFeature(key);
        }

        [Fact]
        public void TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsData()
        {
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            _ = analyticsProcessor.TrackFeature("TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsDataFeature");
            Assert.Equal(1, analyticsProcessor["TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsDataFeature"]);
            _ = analyticsProcessor.TrackFeature("TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsDataFeature");
            Assert.Equal(2, analyticsProcessor["TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsDataFeature"]);
        }
        [Fact]
        public async Task TestAnalyticsProcessorFlushClearsAnalyticsData()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, _defaultAnalyticsUri);
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorFlushClearsAnalyticsDataFeature");
            await analyticsProcessor.Flush();
            Assert.False(analyticsProcessor.HasTrackingItemsInCache());
        }
        [Fact]
        public async void TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsData()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, analyticsUri: _defaultAnalyticsUri);
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature1");
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature2");
            var jObject = JObject.Parse(analyticsProcessor.ToString());
            await analyticsProcessor.Flush();
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
            Assert.Equal(1, jObject["TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature1"].Value<int>());
            Assert.Equal(1, jObject["TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature2"].Value<int>());
        }
        [Fact]
        public async Task TestAnalyticsProcessorPostsToConfiguredAnalyticsUri()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsUri = new Uri("https://analytics.example.com/api/v1/analytics/flags/");
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, Fixtures.ApiKey, analyticsUri);
            await analyticsProcessor.TrackFeature("some_feature");
            await analyticsProcessor.Flush();
            mockHttpClient.Verify(x => x.SendAsync(
                It.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == analyticsUri),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task TestAnalyticsProcessorBaseApiUrlConstructorPostsToAnalyticsEndpoint()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessor(mockHttpClient.Object, Fixtures.ApiKey, _defaultApiUri.ToString());
            await analyticsProcessor.TrackFeature("some_feature");
            await analyticsProcessor.Flush();
            VerifyAnalyticsPostedTo(mockHttpClient, _defaultAnalyticsUri);
        }
        [Fact]
        public async Task TestFlagsmithClientPostsAnalyticsToExpectedUri()
        {
            var customAnalyticsUri = new Uri("https://analytics.example.com/api/v1/analytics/flags/");
            // A client using the default analytics endpoint, relative to the API URI...
            var (defaultUriMock, defaultUriFlags) = await GetClientFlags(analyticsUri: null);
            // ...and a client with an analytics endpoint configured separately from the API URI
            var (customUriMock, customUriFlags) = await GetClientFlags(customAnalyticsUri);
            await Task.Delay(11 * 1000);
            await defaultUriFlags.IsFeatureEnabled("some_feature");
            await customUriFlags.IsFeatureEnabled("some_feature");
            // Tracking a feature does not await the flush it triggers, so give the requests time to be made
            await Task.Delay(2 * 1000);
            VerifyAnalyticsPostedTo(defaultUriMock, _defaultAnalyticsUri);
            VerifyAnalyticsPostedTo(customUriMock, customAnalyticsUri);
        }
        private static async Task<(Mock<HttpClient>, IFlags)> GetClientFlags(Uri analyticsUri)
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var client = new FlagsmithClient(new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                ApiUri = _defaultApiUri,
                AnalyticsUri = analyticsUri,
                HttpClient = mockHttpClient.Object,
                EnableAnalytics = true
            });
            return (mockHttpClient, await client.GetEnvironmentFlags());
        }
        private static void VerifyAnalyticsPostedTo(Mock<HttpClient> mockHttpClient, Uri expectedUri)
            => mockHttpClient.Verify(x => x.SendAsync(
                It.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
                It.IsAny<CancellationToken>()), Times.Once);
        [Fact]
        public async Task TestAnalyticsProcessorFlushEarlyExitIfAnalyticsDataIsEmpty()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, analyticsUri: _defaultAnalyticsUri);
            await analyticsProcessor.Flush();
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task TestAnalyticsProcessorCallingTrackFeatureCallsFlushWhenTimerRunsOut()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, analyticsUri: _defaultAnalyticsUri);
            await Task.Delay(12 * 1000);
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorCallingTrackFeatureCallsFlushWhenTimerRunsOutFeature");
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
        }

        [Fact]
        public async Task TestAnalyticsProcessorDataConcurrentAccess()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, _defaultAnalyticsUri);
            const int numberOfCalls = 100000;

            // When 
            var tasks = new Task[numberOfCalls];

            for (int i = 0; i < tasks.Length; i++)
            {
                var local = i;
                tasks[i] = Task.Run(async () =>
                {
                    await analyticsProcessor.TrackFeature($"Feature {local}");
                });
            }

            await Task.WhenAll(tasks);

            // Then
            Assert.True(true);
        }
    }
}
