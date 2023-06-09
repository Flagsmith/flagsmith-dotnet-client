using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Feature.Models;
using Newtonsoft.Json;
namespace FlagsmithEngine.Segment.Models
{
    public class SegmentModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("rules")]
        public List<SegmentRuleModel> Rules { get; set; } = new List<SegmentRuleModel>();
        [JsonProperty("feature_states")]
        public List<FeatureStateModel> FeatureStates { get; set; } = new List<FeatureStateModel>();
    }
}
