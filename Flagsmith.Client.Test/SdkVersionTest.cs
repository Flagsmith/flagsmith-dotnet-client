using Flagsmith;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class SdkVersionTest
    {
        [Fact]
        public void TestGetUserAgentReturnsExpectedVersion()
        {
            // x-release-please-start-version
            string expectedVersion = "8.0.2";
            // x-release-please-end

            // When
            var userAgent = SdkVersion.GetUserAgent();

            // Then
            Assert.Equal($"flagsmith-dotnet-sdk/{expectedVersion}", userAgent);
        }
    }
}
