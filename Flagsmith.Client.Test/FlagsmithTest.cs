using FlagsmithEngine.Environment.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Flagsmith.FlagsmithClientTest
{
    public class FlagsmithTest
    {
        [Fact]
        public void TestFlagsmithStartsPollingManagerOnInitIfEnabled()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            var flagsmithClientTest = new FlagsmithClientTest(config);
            Assert.Equal(1, flagsmithClientTest["GetAndUpdateEnvironmentFromApi"]);
        }
        [Fact]
        public async Task TestUpdateEnvironmentSetsEnvironment()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            Assert.True(flagsmithClientTest.IsEnvironmentEmpty());
            await flagsmithClientTest.TriggerEnvironmentUpdate();
            Assert.False(flagsmithClientTest.IsEnvironmentEmpty());
            Assert.True(flagsmithClientTest.IsEnvironmentEqual(Fixtures.Environment));
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsCallsApiWhenNoLocalEnvironment()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flags = await flagsmithClientTest.GetFeatureFlags();
            Assert.Equal(1, flagsmithClientTest["GetFeatureFlagsFromApi"]);
            Assert.True(flags[0].IsEnabled());
            Assert.Equal("some-value", await flags[0].GetValue());
            Assert.Equal("some_feature", flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetEnvironmentFlagsUsesLocalEnvironmentWhenAvailable()
        {
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.FlagsmithConfiguration());
            var flags = await flagsmithClientTest.GetFeatureFlags();
            Assert.Equal(0, flagsmithClientTest["GetFeatureFlagsFromApi"]);
            var fs = Fixtures.Environment.FeatureStates[0];
            Assert.Equal(fs.Enabled, flags[0].IsEnabled());
            Assert.Equal(fs.GetValue(), await flags[0].GetValue());
            Assert.Equal(fs.Feature.Name, flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetIdentityFlagsCallsApiWhenNoLocalEnvironmentNoTraits()
        {

            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flags = await flagsmithClientTest.GetFeatureFlags("identifier");
            Assert.Equal(1, flagsmithClientTest["GetIdentityFlagsFromApi"]);
            Assert.True(flags[0].IsEnabled());
            Assert.Equal("some-value", await flags[0].GetValue());
            Assert.Equal("some_feature", flags[0].GetFeature().GetName());
        }
        [Fact]
        public async Task TestGetIdentityFlagsUsesLocalEnvironmentWhenAvailable()
        {

            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(Fixtures.FlagsmithConfiguration());
            _ = await flagsmithClientTest.GetFeatureFlags("identifier", null);
            Assert.Equal(1, flagsmithClientTest["GetIdentityFlagsFromDocuments"]);
        }
        [Fact]
        public async Task TestRequestConnectionErrorRaisesFlagsmithApiError()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpThrowConnectionError();
            await Assert.ThrowsAsync<FlagsmithAPIError>(async () => await flagsmithClientTest.GetFeatureFlags());
        }
        [Fact]
        public async Task TestNon200ResponseRaisesFlagsmithApiError()
        {
            FlagsmithClientTest.instance = null;
            var config = Fixtures.FlagsmithConfiguration();
            config.ApiUrl = Fixtures.ApiUrl;
            config.EnableClientSideEvaluation = false;
            var flagsmithClientTest = new FlagsmithClientTest(config);
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
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            config.DefaultFlagHandler = (string name) => defaultFlag;
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
                Content = new StringContent("[]")
            });
            var flag = await flagsmithClientTest.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", await flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenEnvironmentFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            config.DefaultFlagHandler = (string name) => defaultFlag;
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiFlagResponse)
            });
            var flag = await flagsmithClientTest.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.NotEqual("some-default-value", await flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsUsedWhenNoIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            config.DefaultFlagHandler = (string name) => defaultFlag;
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
            var flag = await flagsmithClientTest.GetFeatureFlag("some_feature", "identifier");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", await flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagIsNotUsedWhenIdentityFlagsReturned()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            config.DefaultFlagHandler = (string name) => defaultFlag;
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(Fixtures.ApiIdentityResponse)
            });
            var flag = await flagsmithClientTest.GetFeatureFlag("some_feature", "identifier");
            Assert.True(flag.IsEnabled());
            Assert.NotEqual("some-default-value", await flag.GetValue());
        }
        [Fact]
        public async Task TestDefaultFlagsAreUsedIfApiErrorAndDefaultFlagHandlerGiven()
        {
            var defaultFlag = new Flag(null, true, "some-default-value");
            var config = Fixtures.FlagsmithConfiguration();
            config.EnableClientSideEvaluation = false;
            config.DefaultFlagHandler = (string name) => defaultFlag;
            FlagsmithClientTest.instance = null;
            var flagsmithClientTest = new FlagsmithClientTest(config);
            flagsmithClientTest.MockHttpResponse(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
            });
            var flag = await flagsmithClientTest.GetFeatureFlag("some_feature");
            Assert.True(flag.IsEnabled());
            Assert.Equal("some-default-value", await flag.GetValue());
        }
    }

}

