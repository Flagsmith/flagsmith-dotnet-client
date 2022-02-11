using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using FlagsmithEngine.Environment.Models;
using Newtonsoft.Json.Linq;

namespace Flagsmith.DotnetClient.Test
{
    internal class Fixtures
    {
        public static string ApiKey => "text_key";
        public static string ApiUrl => "http://test_url/";
        public static AnalyticsProcessorTest GetAnalyticalProcessorTest() => new AnalyticsProcessorTest(new HttpClient(), ApiKey, ApiUrl);
        public static FlagsmithConfiguration FlagsmithConfiguration() => new FlagsmithConfiguration { EnableClientSideEvaluation = true, EnvironmentKey = ApiKey };
        public static JObject JsonObject = JObject.Parse(@"{
 'api_key': '8KzETdDeMY7xkqkSkY3Gsg',
 'heap_config': {
  'base_url': null,
  'api_key': '2539064755'
 },
 'mixpanel_config': {
  'base_url': null,
  'api_key': '8915a3ca693726890e0c58bd256945cc'
 },
 'amplitude_config': {
  'base_url': '',
  'api_key': '00c348027841be8f129bba0fd307a790'
 },
 'feature_states': [
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '3e4ab0c5-888d-499d-a11c-51a0f31bb610',
   'feature_state_value': null,
   'feature': {
    'name': 'try_it',
    'type': 'STANDARD',
    'id': 24
   },
   'enabled': true,
   'django_id': 58
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '12629a83-f6ea-449c-b9fb-a5b6a9aec210',
   'feature_state_value': '[\n  {\n    \'value\': \'EQUAL\',\n    \'label\': \'Exactly Matches (=)\'\n  },\n  {\n    \'value\': \'NOT_EQUAL\',\n    \'label\': \'Does not match (!=)\'\n  },\n  {\n    \'value\': \'PERCENTAGE_SPLIT\',\n    \'label\': \'% Split\'\n  },\n  {\n    \'value\': \'GREATER_THAN\',\n    \'label\': \'>\'\n  },\n  {\n    \'value\': \'GREATER_THAN_INCLUSIVE\',\n    \'label\': \'>=\'\n  },\n  {\n    \'value\': \'LESS_THAN\',\n    \'label\': \'<\'\n  },\n  {\n    \'value\': \'LESS_THAN_INCLUSIVE\',\n    \'label\': \'<=\'\n  },\n  {\n    \'value\': \'CONTAINS\',\n    \'label\': \'Contains\'\n  },\n  {\n    \'value\': \'NOT_CONTAINS\',\n    \'label\': \'Does not contain\'\n  },\n  {\n    \'value\': \'REGEX\',\n    \'label\': \'Matches regex\'\n  }\n]',
   'feature': {
    'name': 'segment_operators',
    'type': 'STANDARD',
    'id': 1530
   },
   'enabled': true,
   'django_id': 5815
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'ae91bfe9-53b3-4344-a4f7-f7cdc57ffb0e',
   'feature_state_value': 'red',
   'feature': {
    'name': 'demo_feature',
    'type': 'STANDARD',
    'id': 2509
   },
   'enabled': false,
   'django_id': 11065
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'd6ef500b-eb8d-46eb-b8fe-845f9a989658',
   'feature_state_value': '{\n  \'url\': \'https://github.com/login/oauth/authorize?scope=user&client_id=5d99dd45d6cdf4a4ac61&redirect_uri=https%3A%2F%2Fdev.bullet-train.io%2Foauth%2Fgithub\'\n}',
   'feature': {
    'name': 'oauth_github',
    'type': 'STANDARD',
    'id': 2712
   },
   'enabled': true,
   'django_id': 12307
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'a6829efe-8ae3-4be7-a194-718d18680cc5',
   'feature_state_value': '{\n \'clientId\':\'232959427810-br6ltnrgouktp0ngsbs04o14ueb9rch0.apps.googleusercontent.com\',\n \'apiKey\':\'AIzaSyCnHuN-y6BIEAM5vTISXaz3X9GpEPSxWjo\'\n}',
   'feature': {
    'name': 'oauth_google',
    'type': 'STANDARD',
    'id': 2713
   },
   'enabled': true,
   'django_id': 12310
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '9cbad71e-99bf-4bbb-889f-a9216d57b75f',
   'feature_state_value': null,
   'feature': {
    'name': 'plan_based_access',
    'type': 'STANDARD',
    'id': 5538
   },
   'enabled': true,
   'django_id': 27322
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '62e85aea-8d9f-41c7-bc52-8813e3f69abf',
   'feature_state_value': '[\'amplitude\',\'datadog\',\'new-relic\',\'segment\',\'rudderstack\',\'slack\', \'heap\',\'mixpanel\']',
   'feature': {
    'name': 'integrations',
    'type': 'STANDARD',
    'id': 5560
   },
   'enabled': true,
   'django_id': 27418
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'e7b19073-c41e-4415-acb4-66ab7a70e5b9',
   'feature_state_value': '{\n    \'datadog\': {\n        \'perEnvironment\': false,\n        \'image\': \'https://app.flagsmith.com/images/integrations/datadog.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/datadog/\',\n        \'fields\': [\n            {\n                \'key\': \'base_url\',\n                \'label\': \'Base URL\'\n            },\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'logging\'\n        ],\n        \'title\': \'Datadog\',\n        \'description\': \'Sends events to Datadog for when flags are created, updated and removed. Logs are tagged with the environment they came from e.g. production.\'\n    },\n    \'slack\': {\n        \'perEnvironment\': true,\n        \'isOauth\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/slack.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/slack/\',\n        \'tags\': [\n            \'messaging\'\n        ],\n        \'title\': \'Slack\',\n        \'description\': \'Sends messages to Slack when flags are created, updated and removed. Logs are tagged with the environment they came from e.g. production.\'\n    },\n    \'amplitude\': {\n        \'perEnvironment\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/amplitude.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/amplitude/\',\n        \'fields\': [\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'Amplitude\',\n        \'description\': \'Sends data on what flags served to each identity.\'\n    },\n    \'new-relic\': {\n        \'perEnvironment\': false,\n        \'image\': \'https://app.flagsmith.com/images/integrations/new_relic.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/newrelic\',\n        \'fields\': [\n            {\n                \'key\': \'base_url\',\n                \'label\': \'New Relic Base URL\'\n            },\n            {\n                \'key\': \'api_key\',\n                \'label\': \'New Relic API Key\'\n            },\n            {\n                \'key\': \'app_id\',\n                \'label\': \'New Relic Application ID\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'New Relic\',\n        \'description\': \'Sends events to New Relic for when flags are created, updated and removed.\'\n    },\n    \'segment\': {\n        \'perEnvironment\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/segment.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/segment\',\n        \'fields\': [\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'Segment\',\n        \'description\': \'Sends data on what flags served to each identity.\'\n    },\n    \'rudderstack\': {\n        \'perEnvironment\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/rudderstack.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/rudderstack\',\n        \'fields\': [            {\n                \'key\': \'base_url\',\n                \'label\': \'Rudderstack Data Plane URL\'\n            },\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'Rudderstack\',\n        \'description\': \'Sends data on what flags served to each identity.\'\n    },\n    \'heap\': {\n        \'perEnvironment\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/heap.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/heap\',\n        \'fields\': [\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'Heap Analytics\',\n        \'description\': \'Sends data on what flags served to each identity.\'\n    },\n    \'mixpanel\': {\n        \'perEnvironment\': true,\n        \'image\': \'https://app.flagsmith.com/images/integrations/mixpanel.svg\',\n        \'docs\': \'https://docs.flagsmith.com/integrations/mixpanel\',\n        \'fields\': [\n            {\n                \'key\': \'api_key\',\n                \'label\': \'API Key\'\n            }\n        ],\n        \'tags\': [\n            \'analytics\'\n        ],\n        \'title\': \'Mixpanel\',\n        \'description\': \'Sends data on what flags served to each identity.\'\n    }\n}',
   'feature': {
    'name': 'integration_data',
    'type': 'STANDARD',
    'id': 5564
   },
   'enabled': true,
   'django_id': 27481
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'b457a3bf-fe7a-4066-a5fc-7fb6bb04857e',
   'feature_state_value': null,
   'feature': {
    'name': 'usage_chart',
    'type': 'STANDARD',
    'id': 6006
   },
   'enabled': true,
   'django_id': 29558
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '860cab1a-dcc3-41d7-9bc5-be6338fd9053',
   'feature_state_value': null,
   'feature': {
    'name': 'scaleup_audit',
    'type': 'STANDARD',
    'id': 6903
   },
   'enabled': true,
   'django_id': 34292
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '14bb2fda-cb81-4292-9058-3581cdcc45cd',
   'feature_state_value': '<strong>\nYou are using the develop environment.\n</strong>',
   'feature': {
    'name': 'butter_bar',
    'type': 'STANDARD',
    'id': 7168
   },
   'enabled': true,
   'django_id': 35671
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '8ee069aa-cc13-4531-9847-5998cafa07b3',
   'feature_state_value': null,
   'feature': {
    'name': 'flag_analytics',
    'type': 'STANDARD',
    'id': 7460
   },
   'enabled': true,
   'django_id': 37186
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '008a9cf1-b55d-42cd-b9f7-fcbf25ce43e4',
   'feature_state_value': null,
   'feature': {
    'name': 'dark_mode',
    'type': 'STANDARD',
    'id': 8266
   },
   'enabled': false,
   'django_id': 41662
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'e55a1f69-b791-4e86-9005-23e3f29b4183',
   'feature_state_value': null,
   'feature': {
    'name': 'read_only_mode',
    'type': 'STANDARD',
    'id': 8798
   },
   'enabled': false,
   'django_id': 44942
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'c196104c-51b4-46c8-8e8b-dbf83cf4274c',
   'feature_state_value': null,
   'feature': {
    'name': 'saml',
    'type': 'STANDARD',
    'id': 10202
   },
   'enabled': true,
   'django_id': 53307
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'b54d8666-82b7-4b91-bb0e-7ec1ad5dd131',
   'feature_state_value': null,
   'feature': {
    'name': 'payments_enabled',
    'type': 'STANDARD',
    'id': 11639
   },
   'enabled': true,
   'django_id': 62248
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'd50185d1-b990-4f00-8b38-67c4d818eafc',
   'feature_state_value': null,
   'feature': {
    'name': 'disable_create_org',
    'type': 'STANDARD',
    'id': 11727
   },
   'enabled': false,
   'django_id': 62798
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '606dbb2b-ae1f-4aa2-a0b6-9fb25687a725',
   'feature_state_value': null,
   'feature': {
    'name': 'prevent_fetch',
    'type': 'STANDARD',
    'id': 13093
   },
   'enabled': false,
   'django_id': 69815
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'a71bdf89-cb1d-40bf-84a6-b04ccae3c77a',
   'feature_state_value': null,
   'feature': {
    'name': 'restrict_project_create_to_admin',
    'type': 'STANDARD',
    'id': 13507
   },
   'enabled': false,
   'django_id': 72075
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '7aa7b96e-844f-44aa-a3de-e2f3471b37dc',
   'feature_state_value': '',
   'feature': {
    'name': 'upgrade_subscription',
    'type': 'STANDARD',
    'id': 14884
   },
   'enabled': false,
   'django_id': 78232
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '53eb916d-07f0-46c6-a481-60ca08a2fcd2',
   'feature_state_value': null,
   'feature': {
    'name': 'compare_environments',
    'type': 'STANDARD',
    'id': 15278
   },
   'enabled': true,
   'django_id': 80127
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'c52232a1-7dce-4691-96b5-aae93b614fb4',
   'feature_state_value': '[\'javascript\']',
   'feature': {
    'name': 'not_operator',
    'type': 'STANDARD',
    'id': 15372
   },
   'enabled': true,
   'django_id': 80523
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'f8cd78a0-d2cc-4c3f-ac89-f17e32d750f8',
   'feature_state_value': null,
   'feature': {
    'name': 'force_2fa',
    'type': 'STANDARD',
    'id': 15394
   },
   'enabled': false,
   'django_id': 80602
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '415ebc10-1f7e-4bb4-b3a7-062149312909',
   'feature_state_value': null,
   'feature': {
    'name': 'disable_oauth_registration',
    'type': 'STANDARD',
    'id': 15875
   },
   'enabled': true,
   'django_id': 82618
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '36b307f6-0830-48f3-994a-9e404a209c2b',
   'feature_state_value': 'Adds the UPDATE_FEATURE_STATE permission to the environment',
   'feature': {
    'name': 'update_feature_state_permission',
    'type': 'STANDARD',
    'id': 16077
   },
   'enabled': false,
   'django_id': 83449
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '98e7e09b-407f-42e5-9be5-46e306f25990',
   'feature_state_value': null,
   'feature': {
    'name': 'organisation_permissions',
    'type': 'STANDARD',
    'id': 16404
   },
   'enabled': false,
   'django_id': 84542
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'a757417b-8d87-49d0-8f74-8310d75a5b03',
   'feature_state_value': null,
   'feature': {
    'name': 'kill_switch',
    'type': 'STANDARD',
    'id': 16798
   },
   'enabled': false,
   'django_id': 86638
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': 'e5522ba1-09d5-476d-9aee-7bb03bd56f25',
   'feature_state_value': 20,
   'feature': {
    'name': 'audit_api_search',
    'type': 'STANDARD',
    'id': 16814
   },
   'enabled': false,
   'django_id': 86730
  },
  {
   'multivariate_feature_state_values': [],
   'featurestate_uuid': '6c266ee9-425c-4fb4-96ce-4f1ab7ccdba8',
   'feature_state_value': null,
   'feature': {
    'name': 'mailing_list',
    'type': 'STANDARD',
    'id': 16857
   },
   'enabled': false,
   'django_id': 86857
  }
 ],
 'project': {
  'name': 'Flagsmith Website',
  'organisation': {
   'name': 'Flagsmith',
   'feature_analytics': false,
   'id': 13,
   'persist_trait_data': true,
   'stop_serving_flags': false
  },
  'id': 12,
  'hide_disabled_flags': false,
  'segments': [
   {
    'name': 'power_user',
    'rules': [],
    'id': 2,
    'feature_states': []
   },
   {
    'name': 'bullet_train_internal_users',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': 'nightwatch@solidstategroup.com',
          'operator': 'NOT_EQUAL',
          'property_': 'email'
         }
        ],
        'rules': []
       },
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '.*@solidstategroup\\.com',
          'operator': 'REGEX',
          'property_': 'email'
         },
         {
          'value': '.*@flagsmith\\.com',
          'operator': 'REGEX',
          'property_': 'email'
         },
         {
          'value': '.*@bullet-train\\.io',
          'operator': 'REGEX',
          'property_': 'email'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 3,
    'feature_states': [
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '3f428864-8680-49fa-8917-7345fdce9df8',
      'feature_state_value': '',
      'feature': {
       'name': 'try_it',
       'type': 'STANDARD',
       'id': 24
      },
      'enabled': false,
      'django_id': 75881
     },
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '7aeb12d7-f66b-4d47-b16f-8ec265d76b2f',
      'feature_state_value': '{\n \'clientId\':\'232959427810-br6ltnrgouktp0ngsbs04o14ueb9rch0.apps.googleusercontent.com\',\n \'apiKey\':\'AIzaSyCnHuN-y6BIEAM5vTISXaz3X9GpEPSxWjo\'\n}',
      'feature': {
       'name': 'oauth_google',
       'type': 'STANDARD',
       'id': 2713
      },
      'enabled': true,
      'django_id': 15395
     },
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': 'fefd59a0-cdfd-48cf-9384-9526fc9255dc',
      'feature_state_value': '<strong>Internal user message</strong>',
      'feature': {
       'name': 'butter_bar',
       'type': 'STANDARD',
       'id': 7168
      },
      'enabled': false,
      'django_id': 60750
     },
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '2a94d7cb-0e27-4910-9cac-914ca33da2e6',
      'feature_state_value': '',
      'feature': {
       'name': 'upgrade_subscription',
       'type': 'STANDARD',
       'id': 14884
      },
      'enabled': true,
      'django_id': 83159
     },
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '41f205cf-45bf-4cc3-96b4-e0a605b40e7b',
      'feature_state_value': '',
      'feature': {
       'name': 'compare_environments',
       'type': 'STANDARD',
       'id': 15278
      },
      'enabled': true,
      'django_id': 80169
     }
    ]
   },
   {
    'name': 'segments_beta',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '@stingray.com',
          'operator': 'CONTAINS',
          'property_': 'email'
         },
         {
          'value': '@ft.com',
          'operator': 'CONTAINS',
          'property_': 'email'
         },
         {
          'value': '@kernwerk.de',
          'operator': 'CONTAINS',
          'property_': 'email'
         },
         {
          'value': '@scolab.com',
          'operator': 'CONTAINS',
          'property_': 'email'
         },
         {
          'value': '@gmail.com',
          'operator': 'CONTAINS',
          'property_': 'email'
         },
         {
          'value': '@ig.com',
          'operator': 'CONTAINS',
          'property_': 'email'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 4,
    'feature_states': []
   },
   {
    'name': 'percentage_split_50',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '50',
          'operator': 'PERCENTAGE_SPLIT',
          'property_': ''
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 74,
    'feature_states': []
   },
   {
    'name': 'org-includes-flagsmith',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '\'13\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 1870,
    'feature_states': []
   },
   {
    'name': 'org-over-seat-limit',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '\'7\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 1871,
    'feature_states': []
   },
   {
    'name': 'flagsmith_team',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '.*@flagsmith\\.com',
          'operator': 'REGEX',
          'property_': 'email'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 2051,
    'feature_states': [
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '14d234e6-91ae-4616-a393-9dfd1ded111d',
      'feature_state_value': '',
      'feature': {
       'name': 'demo_feature',
       'type': 'STANDARD',
       'id': 2509
      },
      'enabled': false,
      'django_id': 90533
     }
    ]
   },
   {
    'name': 'dark_mode',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': 'True',
          'operator': 'EQUAL',
          'property_': 'dark_mode'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 2253,
    'feature_states': [
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': 'fb1d32fb-b980-4985-a8d4-1ec676cf41a3',
      'feature_state_value': '',
      'feature': {
       'name': 'dark_mode',
       'type': 'STANDARD',
       'id': 8266
      },
      'enabled': true,
      'django_id': 41664
     }
    ]
   },
   {
    'name': 'org_butterbar',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '\'369\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         },
         {
          'value': '\'-1\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         },
         {
          'value': '\'4673\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 2338,
    'feature_states': []
   },
   {
    'name': 'read_only_organisations',
    'rules': [
     {
      'type': 'ALL',
      'conditions': [],
      'rules': [
       {
        'type': 'ANY',
        'conditions': [
         {
          'value': '\'3784\'',
          'operator': 'CONTAINS',
          'property_': 'oganisations'
         },
         {
          'value': '\'2187\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         },
         {
          'value': '\'3872\'',
          'operator': 'CONTAINS',
          'property_': 'organisations'
         }
        ],
        'rules': []
       }
      ]
     }
    ],
    'id': 2435,
    'feature_states': [
     {
      'multivariate_feature_state_values': [],
      'featurestate_uuid': '21c7d2ae-60c0-4c29-a08f-f1024f0a2e30',
      'feature_state_value': '',
      'feature': {
       'name': 'read_only_mode',
       'type': 'STANDARD',
       'id': 8798
      },
      'enabled': true,
      'django_id': 44968
     }
    ]
   }
  ]
 },
 'id': 23,
 'segment_config': {
  'base_url': null,
  'api_key': 'dL2Whi0tiPizE2xs59SUjG7NpbVyWBDF'
 }
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
