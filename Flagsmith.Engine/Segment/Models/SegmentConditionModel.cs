using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json;
namespace FlagsmithEngine.Segment.Models
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
        public bool EvaluateModulo(object traitValue)
        {
            try
            {
                string[] parts = this.Value.Split('|');
                if (parts.Length != 2) { return false; }

                double divisor = Convert.ToDouble(parts[0]);
                double remainder = Convert.ToDouble(parts[1]);

                return Convert.ToDouble(traitValue) % divisor == remainder;
            }
            catch (FormatException)
            {
                return false;
            }
        }

    }
}
