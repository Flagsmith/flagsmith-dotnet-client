using Newtonsoft.Json;

namespace Flagsmith
{
    /// <summary>
    /// The running experiment a flag was evaluated under. Only identity evaluation carries it.
    /// </summary>
    public class ExperimentMetadata
    {
        public ExperimentMetadata() { }

        internal ExperimentMetadata(int id, string name, bool inExperiment)
        {
            Id = id;
            Name = name;
            InExperiment = inExperiment;
        }

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
