using Newtonsoft.Json;
namespace FlagsmithEngine.Organization.Models
{
    public class OrganisationModel
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
        public string UniqueSlug => $"{Id}-{Name}";
    }
}
