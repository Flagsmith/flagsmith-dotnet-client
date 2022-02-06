using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Feature.Models
{
    public class MultivariateFeatureStateValueModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("multivariate_feature_option")]
        public MultivariateFeatureOptionModel MultivariateFeatureOption { get; set; }
        [JsonProperty("percentage_allocation")]
        public float PercentageAllocation { get; set; }
        [JsonProperty("mv_fs_value_uuid")]
        public string MvFsValueUUID { get; set; } = Guid.NewGuid().ToString();
    }
}
