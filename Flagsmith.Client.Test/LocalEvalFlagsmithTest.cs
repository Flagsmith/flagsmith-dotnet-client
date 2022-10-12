using Flagsmith;
using Flagsmith.FlagsmithClientTest;
using Flagsmith.Interfaces;
using FlagsmithEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ClientTest
{
    public class LocalEvalFlagsmithTest
    {
        private static async Task<IFlagsmithClient> CreateLocalEvalClient(HttpClient httpClient, Func<string, IFlag> defaultFlagHandler = null)
        {
            var config = new FlagsmithConfiguration(Fixtures.ApiKey, apiUrl: Fixtures.ApiUrl, defaultFlagHandler: defaultFlagHandler);
            var httpFactory = new HttpClientFactoryMocker(httpClient);
            var restClient = new RestClient(NullLogger<RestClient>.Instance, config, httpFactory);

            var refreshService = new EnvironmentRefreshService(NullLogger<EnvironmentRefreshService>.Instance, config, restClient);
            await refreshService.UpdateEnvironment(CancellationToken.None);

            return new LocalEvalFlagsmithClient(
                NullLogger<LocalEvalFlagsmithClient>.Instance,
                new NullAnalyticsProcessor(NullLogger<NullAnalyticsProcessor>.Instance),
                config,
                refreshService,
                new Engine());
        }

        [Fact]
        public async Task TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = await CreateLocalEvalClient(mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
        }

        [Fact]
        public async Task TestUpdateEnvironmentSetsEnvironment()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = await CreateLocalEvalClient(mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "flags", Times.Never);
        }

        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = await CreateLocalEvalClient(mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).Flags?.ToList();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.NotEmpty(flags);
            Assert.Equal(fs.Enabled, flags[0].Enabled);
            Assert.Equal(fs.GetValue(), flags[0].Value);
            Assert.Equal(fs.Feature.Name, flags[0].Feature.Name);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
        }

        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = await CreateLocalEvalClient(mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
            _ = await flagsmithClientTest.GetIdentityFlags("identifier", null);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
        }

        [Fact]
        public async Task testGetIdentitySegmentsNoTraits()
        {
            // Given
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClient = await CreateLocalEvalClient(mockHttpClient.Object);

            // When
            var segments = await flagsmithClient.GetIdentitySegments("identifier");

            // Then
            Assert.Empty(segments);
        }

        [Fact]
        public async Task testGetIdentitySegmentsWithValidTrait()
        {
            // Given
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClient = await CreateLocalEvalClient(mockHttpClient.Object);

            string identifier = "identifier";
            List<Trait> traits = new List<Trait>() { new Trait(traitKey: "foo", traitValue: "bar") };

            // When
            var segments = await flagsmithClient.GetIdentitySegments(identifier, traits);

            // Then
            Assert.Single(segments);
            Assert.Equal("Test segment", segments.First().Name);
        }
    }
}
