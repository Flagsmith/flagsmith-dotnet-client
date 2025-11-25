using System.Collections.Generic;
using Newtonsoft.Json;

namespace FlagsmithEngine.Segment.Models
{
    public class SegmentRuleModel
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("rules")]
        public List<SegmentRuleModel> Rules { get; set; }
        [JsonProperty("conditions")]
        public List<SegmentConditionModel> Conditions { get; set; } = new List<SegmentConditionModel>();
    }
}
