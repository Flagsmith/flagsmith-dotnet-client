using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class RetryTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task TestConnectionErrorIsRetriedConfiguredTimes(int retries)
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, retries);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(retries + 1));
        }

        [Fact]
        public async Task TestConnectionErrorIsRetriedThreeTimesByDefault()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, null);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(4));
        }

        [Theory]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task TestServerErrorResponseIsRetried(HttpStatusCode statusCode)
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(statusCode, null, false);
            var flagsmithClient = CreateClient(mockHttpClient, 1);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(2));
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task TestClientErrorResponseIsAlsoRetried(HttpStatusCode statusCode)
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(statusCode, null, false);
            var flagsmithClient = CreateClient(mockHttpClient, 1);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestTransientFailureRecoversOnRetry()
        {
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.SetupSequence(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Fixtures.ApiFlagResponse) });
            var flagsmithClient = CreateClient(mockHttpClient, 1);

            var flags = await flagsmithClient.GetEnvironmentFlags();

            Assert.True(await flags.IsFeatureEnabled("some_feature"));
            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(2));
        }

        [Fact]
        public async Task TestRequestTimeoutIsNotRetried()
        {
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException());
            var flagsmithClient = CreateClient(mockHttpClient, 2);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
        }

        private static FlagsmithClient CreateClient(Mock<HttpClient> mockHttpClient, int? retries) =>
            new(new FlagsmithConfiguration
            {
                EnvironmentKey = Fixtures.ApiKey,
                HttpClient = mockHttpClient.Object,
                Retries = retries
            });
    }
}
