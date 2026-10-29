using Newtonsoft.Json;

namespace Flagsmith
{
    /// <summary>
    /// The running experiment a flag was evaluated under. Only remote identity evaluation carries it.
    /// </summary>
    public class ExperimentMetadata
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Whether the identity is enrolled in the experiment.
        /// </summary>
        [JsonProperty("in_experiment")]
        public bool InExperiment { get; private set; }
    }
}
