using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using System.Threading.Tasks;

namespace Flagsmith.DotnetClient.Test
{
    public class PollingManagerTest
    {
        [Fact]
        public void TestPollingManagerCallsUpdateEnvironmentOnStart()
        {
            FlagsmithClientTest.instance = null;
            var x = new FlagsmithClientTest(Fixtures.FlagsmithConfiguration());
            Assert.Equal(1, x["GetAndUpdateEnvironmentFromApi"]);
        }
        [Fact]
        public async Task TestPollingManagerCallsUpdateEnvironmentOnEachRefresh()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.EnvironmentRefreshIntervalSeconds = 1;
            var x = new FlagsmithClientTest(config);
            await Task.Delay(2500);
            Assert.Equal(3, x["GetAndUpdateEnvironmentFromApi"]);
        }
    }
}
