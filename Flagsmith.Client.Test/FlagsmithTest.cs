using Flagsmith.Caching;
using Flagsmith.Caching.Impl;
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
        [Fact]
        public async Task TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }

        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = await (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
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
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = await (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
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
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            var flags = await (await flagsmithClientTest.GetIdentityFlags("identifier")).AllFlags();
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
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            _ = await flagsmithClientTest.GetIdentityFlags("identifier", null);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }

        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }

        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            ICache _cache = new MemoryCache();
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }

        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            ICache _cache = new MemoryCache();
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            ICache _cache = new MemoryCache();
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            ICache _cache = new MemoryCache();
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            ICache _cache = new MemoryCache();
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            ICache _cache = new MemoryCache();
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey, defaultFlagHandler: x => defaultFlag), mockHttpClient.Object);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestGetIdentityFlagsSendsTraits()
        {
            ICache _cache = new MemoryCache();
            string identifier = "identifier";
            var traits = new List<Trait>() { new Trait("foo", "bar"), new Trait("ifoo", 1) };

            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });
            var flagsmithClient = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

            var flags = await flagsmithClient.GetIdentityFlags(identifier, traits);

            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            // TODO: verify the body is correct - I've verified manually but can't verify programmatically
        }

        [Fact]
        public async Task testGetIdentitySegmentsNoTraits()
        {
            ICache _cache = new MemoryCache();
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            FlagsmithClient flagsmithClient = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

            // When
            var segments = await flagsmithClient.GetIdentitySegments("identifier");

            // Then
            Assert.Empty(segments);
        }

        [Fact]
        public async Task testGetIdentitySegmentsWithValidTrait()
        {
            ICache _cache = new MemoryCache();
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            FlagsmithClient flagsmithClient = new FlagsmithClient(null, _cache, FlagsmithConfiguration.From(Fixtures.ApiKey), mockHttpClient.Object);

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
