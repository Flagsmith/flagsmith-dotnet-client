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
            bool isCalled = false;
            Func<Task> callback = delegate ()
            {
                isCalled = true;
                return Task.CompletedTask;
            };
            var x = new PollingManager(callback);
            _ = x.StartPoll();
            Assert.True(isCalled);
            x.StopPoll();
        }
        [Fact]
        public async Task TestPollingManagerCallsUpdateEnvironmentOnEachRefresh()
        {
            int calledCount = 0;
            Func<Task> callback = delegate ()
            {
                calledCount += 1;
                return Task.CompletedTask;
            };
            var x = new PollingManager(callback, 1);
            _ = x.StartPoll();
            await Task.Delay(3000);
            Assert.Equal(3, calledCount);
            x.StopPoll();
        }
    }
}
