using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using FlagsmithEngine.Feature.Models;

namespace FlagsmithEngine.Identity.Models
{
    public class IdentityModel
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
        [JsonProperty("identity_features")]
        public IdentityFeaturesList IdentityFeatures { get; set; }
    }
}
