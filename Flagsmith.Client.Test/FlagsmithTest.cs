using ClientTest;
using Flagsmith.Caching.Impl;
using Flagsmith.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        private static IFlagsmithClient CreateClient(IFlagsmithClientConfig config, HttpClient client)
        {
            var cache = new MemoryCache();
            var factory = new HttpClientFactoryMocker(client);
            var analylitcs = new FakeAnalyticsProcessor(null);
            return new FlagsmithClient(null, cache, analylitcs, config, factory);
        }

        [Fact]
        public async Task TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }

        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).Flags;
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var firstFlag = flags.FirstOrDefault();
            Assert.NotNull(firstFlag);
            Assert.True(firstFlag.Enabled);
            Assert.Equal("some-value", firstFlag.Value);
            Assert.Equal("some_feature", firstFlag.Feature.Name);
        }

        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).Flags;
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var firstFlag = flags.FirstOrDefault();
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.NotNull(firstFlag);
            Assert.Equal(fs.Enabled, firstFlag.Enabled);
            Assert.Equal(fs.GetValue(), firstFlag.Value);
            Assert.Equal(fs.Feature.Name, firstFlag.Feature.Name);
        }

        [Fact]
        public async Task TestGetIdentityFlagsCallsApiWhenNoLocalEnvironmentNoTraits()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetIdentityFlags("identifier")).Flags;
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var firstFlag = flags.FirstOrDefault();
            Assert.NotNull(firstFlag);
            Assert.True(firstFlag.Enabled);
            Assert.Equal("some-value", firstFlag.Value);
            Assert.Equal("some_feature", firstFlag.Feature.Name);
        }

        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            _ = await flagsmithClientTest.GetIdentityFlags("identifier", null);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }

        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpClientMocker.MockHttpThrowConnectionError();
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }

        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }

        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestGetIdentityFlagsSendsTraits()
        {
            string identifier = "identifier";
            var traits = new List<Trait>() { new Trait("foo", "bar"), new Trait("ifoo", 1) };

            var mockHttpClient = HttpClientMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });
            var flagsmithClient = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

            var flags = await flagsmithClient.GetIdentityFlags(identifier, traits);

            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            // TODO: verify the body is correct - I've verified manually but can't verify programmatically
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
            var flagsmithClient = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

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
            var flagsmithClient = CreateClient(FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

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
