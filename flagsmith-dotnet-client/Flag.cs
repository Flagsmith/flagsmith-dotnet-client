using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Flag
    {
        [JsonProperty]
        private Feature feature = null;

        [JsonProperty]
        private bool enabled = false;

        [JsonProperty("feature_state_value")]
        private string value = null;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public Feature GetFeature()
        {
            return feature;
        }

        public bool IsEnabled()
        {
            return enabled;
        }

        public string GetValue()
        {
            return value;
        }
    }
}
