using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Feature.Models
{
    public class FeatureModel
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            FeatureModel featureModel = (FeatureModel)obj;
            return this.Id.Equals(featureModel.Id);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }
    }
}
