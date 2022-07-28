using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using FlagsmithEngine.Environment.Models;
using Newtonsoft.Json.Linq;

namespace Flagsmith.FlagsmithClientTest
{
    internal class Fixtures
    {
        public static string ApiKey => "test_key";
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
      'featurestate_uuid': '40eb539d-3713-4720-bbd4-829dbef10d51',
      'feature': {
        'name': 'some_feature',
        'type': 'STANDARD',
        'id': 1
      },
      'segment_id': null,
      'enabled': true
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

    }
}
