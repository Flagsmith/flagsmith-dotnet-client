using System.Collections.Generic;
using System.Net.Http;
using FlagsmithEngine.Environment.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flagsmith.FlagsmithClientTest
{
    internal class Fixtures
    {
        public static string ApiKey => "ser.test_key";
        public static string ApiUrl => "http://test_url/";
        public static AnalyticsProcessorTest GetAnalyticalProcessorTest() => new(new HttpClient(), ApiKey, ApiUrl);
        public static JObject JsonObject = JObject.Parse(@"{
  'api_key': 'test_key',
  'project': {
    'name': 'Test project',
    'organisation': {
      'feature_analytics': false,
      'name': 'Test Org',
      'id': 1,
      'persist_trait_data': true,
      'stop_serving_flags': false
    },
    'id': 1,
    'hide_disabled_flags': false,
    'segments': [
      {
        'id': 1,
        'name': 'Test segment',
        'rules': [
          {
            'type': 'ALL',
            'rules': [
              {
                'type': 'ALL',
                'rules': [],
                'conditions': [
                  {
                    'operator': 'EQUAL',
                    'property_': 'foo',
                    'value': 'bar'
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  },
  'segment_overrides': [],
  'id': 1,
  'feature_states': [
    {
      'multivariate_feature_state_values': [],
      'feature_state_value': 'some-value',
      'id': 1,
      'featurestate_uuid': '00000000-0000-0000-0000-000000000000',
      'feature': {
        'name': 'some_feature',
        'type': 'STANDARD',
        'id': 1
      },
      'segment_id': null,
      'enabled': true
    },
    {
      'feature_state_value': 'default_value',
      'django_id': 2,
      'featurestate_uuid': '11111111-1111-1111-1111-111111111111',
      'feature': {
        'name': 'mv_feature_with_ids',
        'type': 'MULTIVARIATE',
        'id': 2
      },
      'segment_id': null,
      'enabled': true,
      'multivariate_feature_state_values': [
        {
          'id': 100,
          'multivariate_feature_option': {
            'id': 10,
            'value': 'variant_a'
          },
          'mv_fs_value_uuid': 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          'percentage_allocation': 30.0
        },
        {
          'id': 200,
          'multivariate_feature_option': {
            'id': 20,
            'value': 'variant_b'
          },
          'mv_fs_value_uuid': 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          'percentage_allocation': 70.0
        }
      ]
    },
    {
      'feature_state_value': 'fallback_value',
      'django_id': 3,
      'featurestate_uuid': '22222222-2222-2222-2222-222222222222',
      'feature': {
        'name': 'mv_feature_without_ids',
        'type': 'MULTIVARIATE',
        'id': 3
      },
      'segment_id': null,
      'enabled': false,
      'multivariate_feature_state_values': [
        {
          'multivariate_feature_option': {
            'id': 40,
            'value': 'option_y'
          },
          'mv_fs_value_uuid': 'yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy',
          'percentage_allocation': 50.0
        },
        {
          'multivariate_feature_option': {
            'id': 30,
            'value': 'option_x'
          },
          'mv_fs_value_uuid': 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx',
          'percentage_allocation': 25.0
        },
        {
          'multivariate_feature_option': {
            'id': 50,
            'value': 'option_z'
          },
          'mv_fs_value_uuid': 'zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz',
          'percentage_allocation': 25.0
        }
      ]
    }
  ],
  'identity_overrides': [
    {
      'identifier': 'overridden-id',
      'identity_uuid': '0f21cde8-63c5-4e50-baca-87897fa6cd01',
      'created_date': '2019-08-27T14:53:45.698555Z',
      'updated_at': '2023-07-14T16:12:00.000000',
      'environment_api_key': 'test_key',
      'identity_features': [
        {
          'id': 1,
          'feature': {
            'id': 1,
            'name': 'some_feature',
            'type': 'STANDARD'
          },
          'featurestate_uuid': '1bddb9a5-7e59-42c6-9be9-625fa369749f',
          'feature_state_value': 'some-overridden-value',
          'enabled': false,
          'django_id': null,
          'environment': 1,
          'identity': null,
          'feature_segment': null
        }
      ]
    }
  ]
}");
        public static EnvironmentModel Environment { get; } = JsonObject.ToObject<EnvironmentModel>();
        public static string ApiFlagResponse => @"[
    {
        'id': 1,
        'feature': {
            'id': 1,
            'name': 'some_feature',
            'created_date': '2019-08-27T14:53:45.698555Z',
            'initial_value': null,
            'description': null,
            'default_enabled': false,
            'type': 'STANDARD',
            'project': 1
        },
        'feature_state_value': 'some-value',
        'enabled': true,
        'environment': 1,
        'identity': null,
        'feature_segment': null
    }
]";
        public static string ApiIdentityResponse => @"{
    'traits': [
        {
            'id': 1,
            'trait_key': 'some_trait',
            'trait_value': 'some_value'
        }
    ],
    'flags': [
        {
            'id': 1,
            'feature': {
                'id': 1,
                'name': 'some_feature',
                'created_date': '2019-08-27T14:53:45.698555Z',
                'initial_value': null,
                'description': null,
                'default_enabled': false,
                'type': 'STANDARD',
                'project': 1
            },
            'feature_state_value': 'some-value',
            'enabled': true,
            'environment': 1,
            'identity': null,
            'feature_segment': null
        }
    ]
}";

        public static string ApiFlagResponseWithTenFlags
        {
            get
            {
                List<object> flags = new List<object>();
                // Add ten flags named Feature_1, Feature_2, ..., Feature_10
                for (int i = 1; i <= 10; i++)
                {
                    object flag = new
                    {
                        id = i,
                        feature = new
                        {
                            id = i,
                            name = $"Feature_{i}",
                            created_date = "2019-08-27T14 =53 =45.698555Z",
                            default_enabled = false,
                            type = "STANDARD",
                            project = 1
                        },
                        feature_state_value = "some-value",
                        enabled = true,
                        environment = 1,
                    };
                    flags.Add(flag);
                }
                ;
                var json = JsonConvert.SerializeObject(flags);
                // Return the JSON string representation of the flags list
                return json;
            }
        }

        public static string ApiIdentityWithTransientTraitsResponse => @"{
      'traits': [
          {
              'id': 1,
              'trait_key': 'some_trait',
              'trait_value': 'some_value'
          },
          {
              'id': 2,
              'trait_key': 'transient_trait',
              'trait_value': 'transient_trait_value',
              'transient': true
          }
      ],
      'flags': [
          {
              'id': 1,
              'feature': {
                  'id': 1,
                  'name': 'some_feature',
                  'created_date': '2019-08-27T14:53:45.698555Z',
                  'initial_value': null,
                  'description': null,
                  'default_enabled': false,
                  'type': 'STANDARD',
                  'project': 1
              },
              'feature_state_value': 'some-transient-trait-value',
              'enabled': true,
              'environment': 1,
              'identity': null,
              'feature_segment': null
          }
      ]
  }";

        public static string ApiTransientIdentityResponse => @"{
      'traits': [
        {
          'id': 1,
          'trait_key': 'some_trait',
          'trait_value': 'some_value'
        },
      ],
      'flags': [
          {
              'id': 1,
              'feature': {
                  'id': 1,
                  'name': 'some_feature',
                  'created_date': '2019-08-27T14:53:45.698555Z',
                  'initial_value': null,
                  'description': null,
                  'default_enabled': false,
                  'type': 'STANDARD',
                  'project': 1
              },
              'feature_state_value': 'some-identity-trait-value',
              'enabled': true,
              'environment': 1,
              'identity': null,
              'feature_segment': null
          }
      ]
  }";
    }
}
