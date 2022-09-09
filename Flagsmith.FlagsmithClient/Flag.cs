using Flagsmith.Interfaces;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class Flag : IFlag
    {
        public Flag()
        {
        }

        public Flag(Feature feature, bool enabled, string value)
        {
            Enabled = enabled;
            Value = value;
            Feature = feature;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature")]
        public Feature Feature { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("feature_state_value")]
        public string Value { get; set; }

        IFeature IFlag.Feature => Feature;

        public int getFeatureId()
        {
            return Feature?.Id ?? 0;
        }

        public string GetFeatureName()
        {
            return Feature?.Name;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
