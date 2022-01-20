using System;
using System.Collections.Generic;
using System.Text;

namespace Flagsmith_engine.Feature.Models
{
    public class MultivariateFeatureStateValueModel
    {
        public int Id { get; set; }
        public MultivariateFeatureOptionModel MultivariateFeatureOption { get; set; }
        public float PercentageAllocation { get; set; }
        public string MvFsValueUUID { get; set; }
    }
}
