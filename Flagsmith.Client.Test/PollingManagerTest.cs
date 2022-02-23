using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using System.Threading.Tasks;

namespace Flagsmith.FlagsmithClientTest
{
    public class PollingManagerTest
    {
        [Fact]
        public void TestPollingManagerCallsUpdateEnvironmentOnStart()
        {
            var x = new FlagsmithClientTest(Fixtures.ApiKey, enableClientSideEvaluation: true);
            Assert.Equal(1, x["GetAndUpdateEnvironmentFromApi"]);
        }
        [Fact]
        public async Task TestPollingManagerCallsUpdateEnvironmentOnEachRefresh()
        {
            var x = new FlagsmithClientTest(Fixtures.ApiKey, environmentRefreshIntervalSeconds: 1, enableClientSideEvaluation: true);
            await Task.Delay(2500);
            Assert.Equal(3, x["GetAndUpdateEnvironmentFromApi"]);
        }
    }
}
