using System.Text.RegularExpressions;
using Newtonsoft.Json;
namespace Flagsmith_engine.Segment.Models
{
    public class SegmentConditionModel
    {
        [JsonProperty("operator")]
        public string Operator { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("property_")]
        public string Property { get; set; }

        public bool EvaluateNotContains(string traitValue) => !traitValue.Contains(Value);
        public bool EvaluateRegex(string traitValue) => Regex.Match(traitValue, Value).Success;

    }
}
