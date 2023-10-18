using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class IdentityTraitsKey
    {
        public string Identifier { get; set; }
        public List<ITrait> Traits { get; set; }

        public IdentityTraitsKey(string identifier, List<ITrait> traits)
        {
            Identifier = identifier;
            Traits = traits;
        }

        public string GenerateUniqueKey()
        {
            var combinedString = Identifier + JsonConvert.SerializeObject(Traits);
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}