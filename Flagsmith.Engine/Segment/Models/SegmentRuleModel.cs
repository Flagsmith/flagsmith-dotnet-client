using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool MatchingFunction(List<bool> list)
        {
            switch (Type)
            {
                case Constants.AllRule: return list.All(x => x);
                case Constants.AnyRule: return list.Any(x => x);
                case Constants.NoneRule: return !list.Any(x => x);
            }
            throw new Exception("Rule Not Found");
        }
    }
}
