using System;
using Flagsmith;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Flagsmith.FlagsmithClientTest
{
    public class AnalyticsTests
    {
        const string _defaultApiUrl = "https://edge.api.flagsmith.com/api/v1/";
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
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, null);
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
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, baseApiUrl: _defaultApiUrl);
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature1");
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature2");
            var jObject = JObject.Parse(analyticsProcessor.ToString());
            await analyticsProcessor.Flush();
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
            Assert.Equal(1, jObject["TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature1"].Value<int>());
            Assert.Equal(1, jObject["TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsDataFeature2"].Value<int>());
        }
        [Fact]
        public async Task TestAnalyticsProcessorFlushEarlyExitIfAnalyticsDataIsEmpty()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, baseApiUrl: _defaultApiUrl);
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
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, baseApiUrl: _defaultApiUrl);
            await Task.Delay(12 * 1000);
            await analyticsProcessor.TrackFeature("TestAnalyticsProcessorCallingTrackFeatureCallsFlushWhenTimerRunsOutFeature");
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
        }

        [Fact]
        public async Task TestAnalyticsProcessorDataConcurrentAccess()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, null);
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
