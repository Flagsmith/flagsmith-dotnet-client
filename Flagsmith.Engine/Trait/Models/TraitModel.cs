using Newtonsoft.Json;

namespace FlagsmithEngine.Trait.Models
{
    public class TraitModel
    {
        [JsonProperty("trait_key")]
        public string TraitKey { get; set; }
        [JsonProperty("trait_value")]
        public object TraitValue { get; set; }
    }
}
