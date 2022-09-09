using Flagsmith;
using Flagsmith.Caching.Impl;
using Flagsmith.FlagsmithClientTest;
using Flagsmith.Interfaces;
using FlagsmithEngine;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ClientTest
{
    public class LocalEvalFlagsmithTest
    {
        private static IFlagsmithClient CreateLocalEvalClient(HttpClient httpClient, Func<string, IFlag> defaultFlagHandler = null)
        {
            var config = FlagsmithConfiguration.From(Fixtures.ApiKey, apiUrl: Fixtures.ApiUrl, defaultFlagHandler: defaultFlagHandler);
            var httpFactory = new HttpClientFactoryMocker(httpClient);
            var restClient = new RestClient(FakeLogger<RestClient>.Instance, config, httpFactory);

            return new LocalEvalFlagsmithClient(
                FakeLogger<LocalEvalFlagsmithClient>.Instance,
                new MemoryCache(),
                new FakeAnalyticsProcessor(FakeLogger<FakeAnalyticsProcessor>.Instance),
                config,
                restClient,
                new Engine());
        }

        [Fact]
        public async void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateLocalEvalClient(mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "environment-document", Times.Once);
        }

        [Fact]
        public async void TestUpdateEnvironmentSetsEnvironment()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateLocalEvalClient(mockHttpClient.Object);
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
            var flagsmithClientTest = CreateLocalEvalClient(mockHttpClient.Object);
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
            var flagsmithClientTest = CreateLocalEvalClient(mockHttpClient.Object);
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
            var flagsmithClient = CreateLocalEvalClient(mockHttpClient.Object);

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
            var flagsmithClient = CreateLocalEvalClient(mockHttpClient.Object);

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
