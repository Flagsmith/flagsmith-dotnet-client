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
        [JsonProperty("feature")]
        private Feature Feature { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; private set; }

        [JsonProperty("feature_state_value")]
        public string Value { get; private set; }

        /// <summary>
        /// Key of the multivariate option the identity was served, if any.
        /// </summary>
        [JsonProperty("variant")]
        public string Variant { get; private set; }

        [JsonProperty("metadata")]
        private FlagMetadata Metadata { get; set; }

        /// <summary>
        /// The running experiment the flag was evaluated under, if any.
        /// </summary>
        [JsonIgnore]
        public ExperimentMetadata Experiment => Metadata?.Experiment;

        public string GetFeatureName()
        {
            return this.Feature.Name;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        private class FlagMetadata
        {
            [JsonProperty("experiment")]
            public ExperimentMetadata Experiment { get; set; }
        }
    }
}
