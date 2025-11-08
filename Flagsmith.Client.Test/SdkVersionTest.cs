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

        [Fact]
        public void TestGetUserAgentReturnsCorrectFormat()
        {
            // When
            var userAgent = SdkVersion.GetUserAgent();

            // Then
            Assert.StartsWith("flagsmith-dotnet-sdk/", userAgent);
        }

        [Fact]
        public void TestGetUserAgentReturnsConsistentValue()
        {
            // When
            var userAgent1 = SdkVersion.GetUserAgent();
            var userAgent2 = SdkVersion.GetUserAgent();

            // Then
            Assert.Equal(userAgent1, userAgent2);
        }

        [Fact]
        public void TestGetUserAgentContainsVersion()
        {
            // When
            var userAgent = SdkVersion.GetUserAgent();
            var versionPart = userAgent.Substring("flagsmith-dotnet-sdk/".Length);

            // Then
            Assert.True(
                versionPart.Contains("."),
                $"Version part should contain a dot, but was '{versionPart}'"
            );
        }
    }
}
