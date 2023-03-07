using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace Flagsmith
{
    public class Flag : IFlag
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
        private Feature Feature { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; private set; }

        [JsonProperty("feature_state_value")]
        public string Value { get; private set; }

        public int getFeatureId()
        {
            return this.Feature.Id;
        }
        public string GetFeatureName()
        {
            return this.Feature.Name;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
