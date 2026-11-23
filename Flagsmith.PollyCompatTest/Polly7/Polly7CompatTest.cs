using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flagsmith.FlagsmithClientTest;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Xunit;

namespace Flagsmith.PollyCompatTest
{
    public class Polly7CompatTest
    {
        [Fact]
        public async Task TestFlagsmithClientRetriesAlongsideConsumerPolly7()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, 2);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(3));
        }

        [Fact]
        public async Task TestConsumerPolly7CircuitBreakerWrapsFlagsmithClient()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, 0);
            var circuitBreaker = Policy
                .Handle<FlagsmithAPIError>()
                .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await circuitBreaker.ExecuteAsync(() => flagsmithClient.GetEnvironmentFlags()));
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await circuitBreaker.ExecuteAsync(() => flagsmithClient.GetEnvironmentFlags()));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () => await circuitBreaker.ExecuteAsync(() => flagsmithClient.GetEnvironmentFlags()));

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(2));
        }

        private static FlagsmithClient CreateClient(Mock<HttpClient> mockHttpClient, int retries) =>
            new(new FlagsmithConfiguration
            {
                EnvironmentKey = "ser.test_key",
                HttpClient = mockHttpClient.Object,
                Retries = retries
            });
    }
}
