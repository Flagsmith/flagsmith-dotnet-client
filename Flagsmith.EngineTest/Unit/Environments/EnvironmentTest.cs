using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature;
using System.Linq;
namespace EngineTest.Unit.Environments
{
    public class EnvironmentTest
    {
        [Fact]
        public void TestGetFlagsForEnvironmentReturnsFeatureStatesForEnvironmentPayload()
        {
            var stringValue = "foo";
            var featureWithStringValueName = "feature_with_string_value";
            var payload = JObject.Parse($@"{{
  'id': 1,
  'api_key': 'api-key',
  'project': {{
    'id': 1,
    'name': 'test project',
    'organisation': {{
      'id': 1,
      'name': 'Test Org',
      'stop_serving_flags': false,
      'persist_trait_data': true,
      'feature_analytics': true
    }},
    'hide_disabled_flags': false
  }},
  'feature_states': [
    {{
      'id': 1,
      'enabled': true,
      'feature_state_value': null,
      'feature': {{
        'id': 1,
        'name': 'enabled_feature',
        'type': '{Constants.STANDARD}'
      }}
    }},
    {{
      'id': 2,
      'enabled': false,
      'feature_state_value': null,
      'feature': {{
        'id': 2,
        'name': 'disabled_feature',
        'type': '{Constants.STANDARD}'
      }}
    }},
    {{
      'id': 3,
      'enabled': true,
      'feature_state_value': '{stringValue}',
      'feature': {{
        'id': 3,
        'name': '{featureWithStringValueName}',
        'type': '{Constants.STANDARD}'
      }}
    }}
  ]
}}");
            var environment = payload.ToObject<EnvironmentModel>();
            Assert.Equal(3, environment.FeatureStates.Count);
            var feature = environment.FeatureStates.FirstOrDefault(x => x.Feature.Name == featureWithStringValueName);
            Assert.Equal(stringValue, feature?.Value);
        }
        [Fact]
        public void TestBuildEnvironmentModelWithMultivariateFlag()
        {
            var variate1Value = "value-1";
            var variate2Value = "value-2";
            var payload = JObject.Parse($@"{{
        'id': 1,
        'api_key': 'api-key',
        'project': {{
            'id': 1,
            'name': 'test project',
            'organisation': {{
                'id': 1,
                'name': 'Test Org',
                'stop_serving_flags': false,
                'persist_trait_data': true,
                'feature_analytics': true
            }},
            'hide_disabled_flags': false
        }},
        'feature_states': [
            {{
                'id': 1,
                'enabled': true,
                'feature_state_value': null,
                'feature': {{
                    'id': 1,
                    'name': 'enabled_feature',
                    'type': '{Constants.STANDARD}'
                }},
                'multivariate_feature_state_values': [
                    {{
                        'id': 1,
                        'percentage_allocation': 10.0,
                        'multivariate_feature_option': {{
                            'value': '{variate1Value}'
                        }}
                    }},
                    {{
                        'id': 2,
                        'percentage_allocation': 10.0,
                        'multivariate_feature_option': {{
                            'value': '{variate2Value}',
                            'id': 2
                        }},
                    }}
                ]
            }}
        ]
    }}");
            var environment = payload.ToObject<EnvironmentModel>();
            Assert.Equal(1, environment.FeatureStates.Count);
            Assert.Equal(2, environment.FeatureStates[0].MultivariateFeatureStateValues.Count);
        }
    }
}
