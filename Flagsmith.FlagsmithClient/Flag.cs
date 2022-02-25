using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace Flagsmith
{
    public class Flag
    {
        public Flag() { }
        public Flag(Feature feature, bool enabled, string value, int featureId = default)
        {
            this.Enabled = enabled;
            this.Value = value;
            this.Feature = feature;
        }
        [JsonProperty("id")]
        public int Id { get; private set; }
        [JsonProperty("feature")]
        public Feature Feature { get; private set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; private set; }

        [JsonProperty("feature_state_value")]
        public string Value { get; private set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
