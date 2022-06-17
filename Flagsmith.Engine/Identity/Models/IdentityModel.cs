using FlagsmithEngine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using FlagsmithEngine.Feature.Models;
using System.Linq;

namespace FlagsmithEngine.Identity.Models
{
    public class IdentityModel
    {
        [JsonProperty("identity_uuid")]
        public string IdentityUUID { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
        [JsonProperty("environment_api_key")]
        public string EnvironmentApiKey { get; set; }
        [JsonProperty("created_date")]
        public DateTime CreatedDate { get; set; }
        [JsonProperty("identity_traits")]
        public List<TraitModel> IdentityTraits { get; set; }
        [JsonProperty("identity_features")]
        public IdentityFeaturesList IdentityFeatures { get; set; }
        [JsonProperty("django_id")]
        public int? DjangoId { get; set; }
        public string CompositeKey => GenerateCompositeKey(EnvironmentApiKey, Identifier);

        public string GenerateCompositeKey(string envKey, string identifier) => $"{envKey}_{identifier}";
        public void UpdateTraits(List<TraitModel> traits)
        {
            var existingModels = IdentityTraits.ToDictionary(x => x.TraitKey);
            traits.ForEach(trait =>
            {
                if (trait.TraitValue is null)
                    existingModels.Remove(trait.TraitKey);
                else
                    existingModels[trait.TraitKey] = trait;
            });
            IdentityTraits = existingModels.Values.ToList();
        }
    }
}
