using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureSegmentModel
    {
        public int Id { get; set; }
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }
    }
}
