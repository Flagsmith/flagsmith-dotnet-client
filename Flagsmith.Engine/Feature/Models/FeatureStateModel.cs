using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FlagsmithEngine.Exceptions;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureStateModel
    {
        [JsonProperty(PropertyName = "feature")]
        public FeatureModel Feature { get; set; }
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("feature_state_value")]
        public object Value { get; set; }
        [JsonProperty(PropertyName = "multivariate_feature_state_values")]
        public List<MultivariateFeatureStateValueModel> MultivariateFeatureStateValues { get; set; }
        [JsonProperty(PropertyName = "django_id")]
        public int? DjangoId { get; set; }
        [JsonProperty(PropertyName = "featurestate_uuid")]
        public string FeatureStateUUID { get; set; }
        [JsonProperty(PropertyName = "feature_segment")]
        public FeatureSegmentModel FeatureSegment { get; set; } = null;

        [OnSerialized()]
        private void ValidatePercentageAllocations(StreamingContext _)
        {
            var totalAllocation = MultivariateFeatureStateValues?.Sum(m => m.PercentageAllocation);
            if (totalAllocation > 100)
                throw new InvalidPercentageAllocation("Total percentage allocation should not be more than 100");
        }
    }
}
