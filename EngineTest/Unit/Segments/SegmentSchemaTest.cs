using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Feature.Models;
using feature = FlagsmithEngine.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EngineTest.Unit.Segments
{
    public class SegmentSchemaTest
    {
        [Fact]
        public void TestSegmentSchemaEngineModelObjectToJobject()
        {
            var segment = new SegmentModel()
            {
                Id = 1,
                Name = "segment",
                Rules = new List<SegmentRuleModel> {
                    new SegmentRuleModel{
                        Type=Constants.AllRule,
                        Conditions = new List<SegmentConditionModel> {
                            new SegmentConditionModel { Operator = Constants.Equal, Property = "foo", Value = "bar" }
                        }
                    }
                },
                FeatureStates = new List<FeatureStateModel>
                {
                    new FeatureStateModel{
                        DjangoId = 1,
                        Feature = new FeatureModel{Id = 1, Name = "my_feature", Type = feature.Constants.STANDARD
                        }
                    },
                }
            };
            var jobject = JObject.FromObject(segment);
            Assert.Equal(1, ((JArray)jobject["feature_states"]).Count);
            Assert.Equal(1, ((JArray)jobject["rules"]).Count);
        }
        [Fact]
        public void TestJobjectToSegmentModel()
        {
            var jObject = JObject.Parse(@"{
        'id': 1,
        'name': 'Segment',
        'rules': [
            {
                'rules': [],
                'conditions': [
                    {'operator': 'EQUAL', 'property_': 'foo', 'value': 'bar'}
                ],
                'type': 'ALL',
            }
        ],
        'feature_states': [
            {
                'multivariate_feature_state_values': [],
                'id': 1,
                'enabled': true,
                'feature_state_value': null,
                'feature': {'id': 1, 'name': 'my_feature', 'type': 'STANDARD'},
            }
        ],
    }");
            var segment = jObject.ToObject<SegmentModel>();
            Assert.Equal(jObject["id"].Value<int>(), segment.Id);
            Assert.Equal(1, segment.Rules.Count);
            Assert.Equal(1, segment.FeatureStates.Count);
        }
        [Fact]
        public void TestSegmentConditionSchemaLoadWhenPropertyIsNull()
        {
            var jObject = JObject.Parse($"{{'operator': '{Constants.PercentageSplit}', 'value': 10, 'property_': null}}");
            var segmentCondition = jObject.ToObject<SegmentConditionModel>();
            Assert.Equal(jObject["value"].Value<string>(), segmentCondition.Value);
            Assert.Equal(jObject["operator"].Value<string>(), segmentCondition.Operator);
            Assert.Null(segmentCondition.Property);
        }
    }
}
