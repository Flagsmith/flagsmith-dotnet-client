using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class IdentityWrapper
    {
        public string Identifier { get; }

        public List<ITrait> Traits { get; }

        public bool Transient { get; }

        public string CacheKey
        {
            get
            {
                var combinedString = Identifier + JsonConvert.SerializeObject(Traits);
                return Utils.GetHashString(combinedString);
            }
        }

        public IdentityWrapper(string identifier, List<ITrait> traits, bool transient = false)
        {
            Identifier = identifier;
            Traits = traits;
            Transient = transient;
        }
    }
}