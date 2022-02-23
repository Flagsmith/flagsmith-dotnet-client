using Newtonsoft.Json;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Flag
    {
        public Flag() { }
        public Flag(Feature feature, bool enabled, string value)
        {
            this.Feature = feature;
            this.Enabled = enabled;
            this.Value = value;
        }
        [JsonProperty("featureId")]
        private int FeatureId;
        [JsonProperty("feature")]
        private Feature Feature = null;

        [JsonProperty("enabled")]
        private bool Enabled = false;

        [JsonProperty("feature_state_value")]
        private string Value = null;
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public Feature GetFeature()
        {
            return Feature;
        }

        public bool IsEnabled()
        {
            return Enabled;
        }

        public string GetValue()
        {
            return Value;
        }


    }
}
