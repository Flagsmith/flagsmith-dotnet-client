using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using FlagsmithEngine.Project.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;

namespace FlagsmithEngine.Environment.Models
{
    public class EnvironmentModel
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "api_key")]
        public string ApiKey { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "project")]
        public ProjectModel Project { get; set; }
        [JsonProperty(PropertyName = "feature_states")]
        public List<FeatureStateModel> FeatureStates { get; set; }
        [JsonProperty(PropertyName = "identity_overrides")]
        public List<IdentityModel> IdentityOverrides { get; set; }
    }
}
