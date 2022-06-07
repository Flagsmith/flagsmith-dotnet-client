using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using FlagsmithEngine.Project.Models;
using FlagsmithEngine.Feature.Models;

namespace FlagsmithEngine.Environment.Models
{
    public class EnvironmentModel
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "api_key")]
        public string ApiKey { get; set; }
        [JsonProperty(PropertyName = "project")]
        public ProjectModel Project { get; set; }
        [JsonProperty(PropertyName = "feature_states")]
        public List<FeatureStateModel> FeatureStates { get; set; }
        public IntegrationModel AmplitudeConfig { get; set; }
        public IntegrationModel SegmentConfig { get; set; }
        public IntegrationModel MixpanelConfig { get; set; }
        public IntegrationModel HeapConfig { get; set; }

    }
}
