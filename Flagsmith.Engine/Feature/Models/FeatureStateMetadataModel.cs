using Newtonsoft.Json;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureStateMetadataModel
    {
        /// <summary>
        /// The running experiment on the feature, if any.
        /// </summary>
        [JsonProperty(PropertyName = "experiment")]
        public FeatureStateExperimentModel Experiment { get; set; }
    }

    public class FeatureStateExperimentModel
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        /// <summary>
        /// Whether identities served this feature state are enrolled in the experiment: true for its rollout segment's override.
        /// </summary>
        [JsonProperty(PropertyName = "in_experiment")]
        public bool InExperiment { get; set; }
    }
}
