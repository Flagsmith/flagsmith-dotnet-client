using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        [Fact]
        public void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, enableClientSideEvaluation: true);
            Assert.Equal(1, flagsmithClientTest["GetAndUpdateEnvironmentFromApi"]);
        }
        [Fact]
        public async Task TestUpdateEnvironmentSetsEnvironment()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey);
            Assert.True(flagsmithClientTest.IsEnvironmentEmpty());
            await flagsmithClientTest.TriggerEnvironmentUpdate();
            Assert.False(flagsmithClientTest.IsEnvironmentEmpty());
            Assert.True(flagsmithClientTest.IsEnvironmentEqual(Fixtures.Environment));
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flags = (await flagsmithClientTest.GetFeatureFlags()).AllFlags();
            Assert.Equal(1, flagsmithClientTest["GetFeatureFlagsFromApi"]);
            Assert.True(flags[0].IsEnabled());
            Assert.Equal("some-value", flags[0].GetValue());
            Assert.Equal("some_feature", flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, enableClientSideEvaluation: true);
            var flags = (await flagsmithClientTest.GetFeatureFlags()).AllFlags();
            Assert.Equal(0, flagsmithClientTest["GetFeatureFlagsFromApi"]);
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.Equal(fs.Enabled, flags[0].IsEnabled());
            Assert.Equal(fs.GetValue(), flags[0].GetValue());
            Assert.Equal(fs.Feature.Name, flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsApiWhenNoLocalEnvironmentNoTraits()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flags = (await flagsmithClientTest.GetFeatureFlags("identifier")).AllFlags();
            Assert.Equal(1, flagsmithClientTest["GetIdentityFlagsFromApi"]);
            Assert.True(flags[0].IsEnabled());
            Assert.Equal("some-value", flags[0].GetValue());
            Assert.Equal("some_feature", flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, enableClientSideEvaluation: true);
            _ = await flagsmithClientTest.GetFeatureFlags("identifier", null);
            Assert.Equal(1, flagsmithClientTest["GetIdentityFlagsFromDocuments"]);
        }
        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey);
            flagsmithClientTest.MockHttpThrowConnectionError();
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetFeatureFlags());
        }
        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetFeatureFlags());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, defaultFlagHandler: (string name) => defaultFlag);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
                Content = new StringContent("[]")
            });
            var flags = await flagsmithClientTest.GetFeatureFlags();
            var flag = await flags.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, defaultFlagHandler: (string name) => defaultFlag);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flags = await flagsmithClientTest.GetFeatureFlags();
            var flag = await flags.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.NotEqual("some-default-value", flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, defaultFlagHandler: (string name) => defaultFlag);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flags = await flagsmithClientTest.GetFeatureFlags("identifier");
            var flag = await flags.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, defaultFlagHandler: (string name) => defaultFlag);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flags = await flagsmithClientTest.GetFeatureFlags("identifier");
            var flag = await flags.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.NotEqual("some-default-value", flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.ApiKey, defaultFlagHandler: (string name) => defaultFlag);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flags = await flagsmithClientTest.GetFeatureFlags();
            var flag = await flags.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", flag.GetValue());
        }
    }

}

