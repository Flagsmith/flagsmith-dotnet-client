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
    public class PollyCore8CompatTest
    {
        [Fact]
        public async Task TestFlagsmithClientRetriesAlongsideConsumerPollyCore8()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, 2);

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClient.GetEnvironmentFlags());

            mockHttpClient.VerifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", () => Times.Exactly(3));
        }

        [Fact]
        public async Task TestConsumerPollyCore8CircuitBreakerWrapsFlagsmithClient()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClient = CreateClient(mockHttpClient, 0);
            var pipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<FlagsmithAPIError>(),
                    FailureRatio = 1,
                    MinimumThroughput = 2,
                    BreakDuration = TimeSpan.FromMinutes(1)
                })
                .Build();

            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await pipeline.ExecuteAsync(async _ => await flagsmithClient.GetEnvironmentFlags()));
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await pipeline.ExecuteAsync(async _ => await flagsmithClient.GetEnvironmentFlags()));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () => await pipeline.ExecuteAsync(async _ => await flagsmithClient.GetEnvironmentFlags()));

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
