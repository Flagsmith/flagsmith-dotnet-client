using System;
using Flagsmith;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Flagsmith.DotnetClient.Test
{
    public class AnalyticsTests
    {

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
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            await analyticsProcessor.TrackFeature(1);
            await analyticsProcessor.Flush();
            Assert.False(analyticsProcessor.HasTrackingItemsInCache());
        }
        [Fact]
        public async void TestAnalyticsProcessorFlushPostRequestDataMatchAnanlyticsData()
        {
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            await analyticsProcessor.TrackFeature(1);
            await analyticsProcessor.TrackFeature(2);
            var jObject = JObject.Parse(analyticsProcessor.ToString());
            await analyticsProcessor.Flush();
            Assert.Equal(1, analyticsProcessor["Flush"]);
            Assert.Equal(1, jObject["1"].Value<int>());
            Assert.Equal(1, jObject["2"].Value<int>());
        }
        [Fact]
        public async Task TestAnalyticsProcessorFlushEarlyExitIfAnalyticsDataIsEmpty()
        {
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            await analyticsProcessor.Flush();
            Assert.True(analyticsProcessor.IsFlushEarlyReturn);
        }
        [Fact]
        public async Task TestAnalyticsProcessorCallingTrackFeatureCallsFlushWhenTimerRunsOut()
        {
            var analyticsProcessor = Fixtures.GetAnalyticalProcessorTest();
            await Task.Delay(12 * 1000);
            await analyticsProcessor.TrackFeature(1);
            Assert.Equal(1, analyticsProcessor["Flush"]);
        }
    }
}
