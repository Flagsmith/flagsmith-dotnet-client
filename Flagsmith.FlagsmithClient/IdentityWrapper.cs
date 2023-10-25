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

        public string CacheKey
        {
            get
            {
                var combinedString = Identifier + JsonConvert.SerializeObject(Traits);
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        public IdentityWrapper(string identifier, List<ITrait> traits)
        {
            Identifier = identifier;
            Traits = traits;
        }
    }
}