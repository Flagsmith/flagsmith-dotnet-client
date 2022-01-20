using Flagsmith_engine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Flagsmith_engine.Feature.Models;

namespace Flagsmith_engine.Identity.Models
{
    public class IdentityModel
    {
        [JsonProperty("identity_uuid")]
        public string IdentityUUID { get; set; }
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
        [JsonProperty("environment_api_key")]
        public string EnvironmentApiKey { get; set; }
        [JsonProperty("created_date")]
        public DateTime CreatedDate { get; set; }
        [JsonProperty("identity_traits")]
        public List<TraitModel> IdentityTraits { get; set; }
        [JsonProperty("identity_features")]
        public List<FeatureStateModel> IdentityFeatures { get; set; }
        public string CompositeKey => GenerateCompositeKey(EnvironmentApiKey, Identifier);

        private string GenerateCompositeKey(string envKey, string identifier) => $"{envKey}_{identifier}";


    }
}
