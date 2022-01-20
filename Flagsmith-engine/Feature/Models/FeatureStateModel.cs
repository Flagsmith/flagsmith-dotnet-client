using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Flagsmith_engine.Utils;
using System.Linq;

namespace Flagsmith_engine.Feature.Models
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
        public int DjangoId { get; set; }
        public string FeatureStateUUID { get; set; }
        public object GetValue(int? identityId = null) =>
            identityId.HasValue && MultivariateFeatureStateValues.Count > 0 ? GetMultivariateValue(identityId.Value) : Value;

        public object GetMultivariateValue(int identityId)
        {
            var percentageValue = Hashing.GetHashedPercentageForObjectIds(new List<string>
            {
              DjangoId != 0 ? DjangoId.ToString() : FeatureStateUUID,
              identityId.ToString()
            });
            var startPercentage = 0.0;
            foreach (var myValue in MultivariateFeatureStateValues.OrderBy(m => m.Id))
            {
                var limit = myValue.PercentageAllocation + startPercentage;
                if (startPercentage <= percentageValue && percentageValue < limit)
                    return myValue.MultivariateFeatureOption.Value;
                startPercentage = limit;
            }
            return Value;
        }
    }
}
