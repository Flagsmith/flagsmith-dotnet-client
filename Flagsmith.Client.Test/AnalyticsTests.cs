using System;
using Flagsmith;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Flagsmith.FlagsmithClientTest
{
    public class AnalyticsTests
    {
        const string _defaultApiUrl = "https://api.flagsmith.com/api/v1/";
        [Fact]
        public void TestAnalyticsProcessorTrackFeatureUpdatesAnalyticsData()
        {
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            _ = analyticsProcessor.TrackFeature(1);
            Assert.Equal(1, analyticsProcessor[1]);
            _ = analyticsProcessor.TrackFeature(1);
            Assert.Equal(2, analyticsProcessor[1]);
        }
        [Fact]
        public async Task TestAnalyticsProcessorFlushClearsAnalyticsData()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            });
            var analyticsProcessor = new AnalyticsProcessorTest(mockHttpClient.Object, null, null);
            await analyticsProcessor.TrackFeature(1);
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
            await analyticsProcessor.TrackFeature(1);
            await analyticsProcessor.TrackFeature(2);
            var jObject = JObject.Parse(analyticsProcessor.ToString());
            await analyticsProcessor.Flush();
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
            Assert.Equal(1, jObject["1"].Value<int>());
            Assert.Equal(1, jObject["2"].Value<int>());
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
            await analyticsProcessor.TrackFeature(1);
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/analytics/flags/", Times.Once);
        }
    }
}
