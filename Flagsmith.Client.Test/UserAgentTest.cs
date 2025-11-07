using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class UserAgentTest
    {
        [Fact]
        public async Task TestUserAgentHeaderIsSentInGetEnvironmentFlags()
        {
            // Given
            HttpRequestMessage capturedRequest = null!;

            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    capturedRequest = request;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(Fixtures.ApiFlagResponse)
                });

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = httpClientMock.Object
            };

            var client = new FlagsmithClient(config);

            // When
            await client.GetEnvironmentFlags();

            // Then
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("User-Agent"));

            var userAgentValues = capturedRequest.Headers.GetValues("User-Agent").ToList();
            Assert.Single(userAgentValues);

            var userAgent = userAgentValues[0];
            Assert.StartsWith("flagsmith-dotnet-sdk/", userAgent);

            // Verify it's either a version number or "unknown"
            var versionPart = userAgent.Substring("flagsmith-dotnet-sdk/".Length);
            Assert.True(versionPart == "unknown" || versionPart.Contains("."),
                $"User-Agent should be 'flagsmith-dotnet-sdk/version' or 'flagsmith-dotnet-sdk/unknown', but was '{userAgent}'");
        }

        [Fact]
        public async Task TestUserAgentHeaderIsSentInGetIdentityFlags()
        {
            // Given
            HttpRequestMessage capturedRequest = null!;

            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    capturedRequest = request;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(Fixtures.ApiIdentityResponse)
                });

            var config = new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = httpClientMock.Object
            };

            var client = new FlagsmithClient(config);

            // When
            await client.GetIdentityFlags("test-identity");

            // Then
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("User-Agent"));

            var userAgentValues = capturedRequest.Headers.GetValues("User-Agent").ToList();
            Assert.Single(userAgentValues);
            Assert.StartsWith("flagsmith-dotnet-sdk/", userAgentValues[0]);
        }

        [Fact]
        public void TestSdkVersionReturnsConsistentValue()
        {
            // When
            var userAgent1 = SdkVersion.GetUserAgent();
            var userAgent2 = SdkVersion.GetUserAgent();

            // Then
            Assert.Equal(userAgent1, userAgent2);
            Assert.StartsWith("flagsmith-dotnet-sdk/", userAgent1);
        }

        [Fact]
        public async Task TestUserAgentHeaderIsSentInAnalyticsFlush()
        {
            // Given
            HttpRequestMessage capturedRequest = null!;

            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    capturedRequest = request;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            var analyticsProcessor = new AnalyticsProcessor(
                httpClientMock.Object,
                Fixtures.ApiKey,
                Fixtures.ApiUrl
            );

            // When
            await analyticsProcessor.TrackFeature("test_feature");
            await analyticsProcessor.Flush();

            // Then
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("User-Agent"));

            var userAgentValues = capturedRequest.Headers.GetValues("User-Agent").ToList();
            Assert.Single(userAgentValues);
            Assert.StartsWith("flagsmith-dotnet-sdk/", userAgentValues[0]);
        }
    }
}
