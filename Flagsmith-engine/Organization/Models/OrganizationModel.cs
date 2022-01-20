using Newtonsoft.Json;
namespace Flagsmith_engine.Organization.Models
{
    public class OrganizationModel
    {
        [JsonProperty("persist_trait_data")]
        public bool PersistTraitData { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("feature_analytics")]
        public bool FeatureAnalytics { get; set; }
        [JsonProperty("stop_serving_flags")]
        public bool StopServingFlags { get; set; }
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
