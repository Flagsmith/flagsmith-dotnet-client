using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class SdkVersionTest
    {
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
        public void TestGetUserAgentContainsVersionOrUnknown()
        {
            // When
            var userAgent = SdkVersion.GetUserAgent();
            var versionPart = userAgent.Substring("flagsmith-dotnet-sdk/".Length);

            // Then
            Assert.True(
                versionPart == "unknown" || versionPart.Contains("."),
                $"Version part should be 'unknown' or contain a dot, but was '{versionPart}'"
            );
        }
    }
}
