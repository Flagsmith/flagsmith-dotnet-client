using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureSegmentModel
    {
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }
    }
}
