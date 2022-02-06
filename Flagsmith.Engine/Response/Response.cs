using FlagsmithEngine.Feature.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Models
{
    public class Trait
    {
        public int id { get; set; }
        public string trait_key { get; set; }
        public object trait_value { get; set; }
    }

    public class Flag
    {
        public int id { get; set; }
        public FeatureModel feature { get; set; }
        public object feature_state_value { get; set; }
        public bool enabled { get; set; }
        public int environment { get; set; }
        public object identity { get; set; }
        public int? feature_segment { get; set; }
    }

    public class Response
    {
        public List<Trait> traits { get; set; }
        public List<Flag> flags { get; set; }
    }
}
