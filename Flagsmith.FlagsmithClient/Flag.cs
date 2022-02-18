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
        protected int FeatureId;
        [JsonProperty("feature")]
        protected Feature Feature = null;

        [JsonProperty("enabled")]
        protected bool Enabled = false;

        [JsonProperty("feature_state_value")]
        protected string Value = null;

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

        public virtual Task<string> GetValue()
        {
            return Task.FromResult(Value);
        }

    }
}
