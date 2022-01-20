using System;
using System.Collections.Generic;
using System.Text;
using Flagsmith_engine.Feature.Models;
using Newtonsoft.Json;
namespace Flagsmith_engine.Segment.Models
{
    public class SegmentModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("rules")]
        public List<SegmentRuleModel> Rules { get; set; }
        [JsonProperty("feature_states")]
        public List<FeatureStateModel> FeatureStates { get; set; }
    }
}
