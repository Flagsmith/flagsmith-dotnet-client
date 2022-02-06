using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Identity.Models;
using Newtonsoft.Json.Linq;
using Xunit;
namespace EngineTest.Unit.Identities
{
    public class IdentityBuilderTest
    {
        [Fact]
        public void TestBuildIdentityModelFromJobjectNoFeatureStates()
        {
            var jObject = JObject.Parse(@"{
        'id': 1,
        'identifier': 'test-identity',
        'environment_api_key': 'api-key',
        'created_date': '2021-08-22T06:25:23.406995Z',
        'identity_traits': [{ 'trait_key': 'trait_key', 'trait_value': 'trait_value'}],
    }");
            var identity = jObject.ToObject<IdentityModel>();
            Assert.Null(identity.IdentityFeatures);
            Assert.Single(identity.IdentityTraits);
        }
        [Fact]
        public void TestBuildIdentityModelFromJobjectUsesIdentityFeatureListForIdentityFeatures()
        {
            var jObject = JObject.Parse(@"{
        'id': 1,
        'identifier': 'test-identity',
        'environment_api_key': 'api-key',
        'created_date': '2021-08-22T06:25:23.406995Z',
        'identity_features': [
            {
                'id': 1,
                'feature': {
                    'id': 1,
                    'name': 'test_feature',
                    'type': 'STANDARD',
                },
                'enabled': true,
                'feature_state_value': 'some-value',
            }
        ],
    }");
            jObject.ToObject<IdentityModel>();
            Assert.True(true);
        }
        [Fact]
        public void TestBuildIdentityModelFromJobjectCreatesIdentityUUID()
        {
            var jObject = JObject.Parse(@"{'identifier': 'test_user', 'environment_api_key': 'some_key'}");
            var identity = jObject.ToObject<IdentityModel>();
            Assert.NotEmpty(identity.IdentityUUID);
        }
        [Fact]
        public void TestBuildIdentityModelFromJobjectWithFeatureStates()
        {
            var jObject = JObject.Parse(@"{
        'id': 1,
        'identifier': 'test-identity',
        'environment_api_key': 'api-key',
        'created_date': '2021-08-22T06:25:23.406995Z',
        'identity_features': [
            {
                'id': 1,
                'feature': {
                    'id': 1,
                    'name': 'test_feature',
                    'type': 'STANDARD',
                },
                'enabled': true,
                'feature_state_value': 'some-value',
            }
        ],
    }");
            var identity = jObject.ToObject<IdentityModel>();
            Assert.Single(identity.IdentityFeatures);
        }
        [Fact]
        public void TestIdentityJobjectCreatedUsingModelCanConvertBackToModel()
        {
            var identity = new IdentityModel { EnvironmentApiKey = "some_key", Identifier = "test_identifier" };
            var jObject = JObject.FromObject(identity);
            jObject.ToObject<IdentityModel>();
            Assert.True(true);
        }

    }
}
