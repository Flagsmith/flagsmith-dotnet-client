using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Utils;
using System.Linq;
using System.Runtime.Serialization;
using FlagsmithEngine.Exceptions;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureStateModel
    {
        public Hashing Hashing = new Hashing();

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
        public string FeatureStateUUID { get; set; } = new Guid().ToString();
        [JsonProperty(PropertyName = "feature_segment")]
        public FeatureSegmentModel FeatureSegment { get; set; } = null;
        public object GetValue(string identityId = null) =>
            identityId != null && MultivariateFeatureStateValues?.Count > 0 ? GetMultivariateValue(identityId.ToString()) : Value;

        public object GetMultivariateValue(string identityId)
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

        /// <summary>
        /// Another FeatureStateModel is deemed to be higher priority if and only if 
        /// it has a FeatureSegment and either this.FeatureSegment is null or the 
        /// value of other.FeatureSegment.priority is lower than that of 
        /// this.FeatureSegment.priority. 
        /// </summary>
        public bool IsHigherPriority(FeatureStateModel other)
        {
            if (this.FeatureSegment == null || other.FeatureSegment == null)
            {
                return this.FeatureSegment != null && other.FeatureSegment == null;
            }

            return this.FeatureSegment.Priority < other.FeatureSegment.Priority;
        }
        [OnSerialized()]
        private void ValidatePercentageAllocations(StreamingContext _)
        {
            var totalAllocation = MultivariateFeatureStateValues?.Sum(m => m.PercentageAllocation);
            if (totalAllocation > 100)
                throw new InvalidPercentageAllocation("Total percentage allocation should not be more than 100");
        }
    }
}
