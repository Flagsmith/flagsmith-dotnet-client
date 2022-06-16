using Moq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Newtonsoft.Json;

namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        [Fact]
        public void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, enableClientSideEvaluation: true);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async void TestUpdateEnvironmentSetsEnvironment()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            await flagsmithClientTest.GetEnvironmentFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Never);
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/flags/", Times.Once);
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            var flags = (await flagsmithClientTest.GetEnvironmentFlags()).AllFlags();
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.Equal(fs.Enabled, flags[0].Enabled);
            Assert.Equal(fs.GetValue(), flags[0].Value);
            Assert.Equal(fs.Feature.Name, flags[0].GetFeatureName());
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsApiWhenNoLocalEnvironmentNoTraits()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            var flags = (await flagsmithClientTest.GetIdentityFlags("identifier")).AllFlags();
            Assert.True(flags[0].Enabled);
            Assert.Equal("some-value", flags[0].Value);
            Assert.Equal("some_feature", flags[0].GetFeatureName());
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);

        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.JsonObject.ToString())
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, enableClientSideEvaluation: true, httpClient: mockHttpClient.Object);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);
            _ = await flagsmithClientTest.GetIdentityFlags("identifier", null);
            mockHttpClient.verifyHttpRequest(HttpMethod.Get, "/api/v1/environment-document/", Times.Once);

        }
        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetEnvironmentFlags());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetIdentityFlags("identifier");
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.NotEqual("some-default-value", flag.Value);
        }
        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flagsmithClientTest = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object, defaultFlagHandler: (string name) => defaultFlag);
            var flags = await flagsmithClientTest.GetEnvironmentFlags();
            var flag = await flags.GetFlag("some_feature");
            Assert.True(flag.Enabled);
            Assert.Equal("some-default-value", flag.Value);
        }

        [Fact]
        public async Task TestGetIdentityFlagsSendsTraits()
        {
            string identifier = "identifier";
            var traits = new List<Trait>() { new Trait( "foo", "bar" ), new Trait( "ifoo", 1 ) };
    
            var mockHttpClient = HttpMocker.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });
            var flagsmithClient = new FlagsmithClient(Fixtures.ApiKey, httpClient: mockHttpClient.Object);

            var flags = await flagsmithClient.GetIdentityFlags(identifier, traits);
            
            mockHttpClient.verifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Once);
            // TODO: verify the body is correct - I've verified manually but can't verify programmatically
        }
    }
}

