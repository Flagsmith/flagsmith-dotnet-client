using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Flagsmith
{
    public class IdentityTraitsKey
    {
        public string Identity { get; set; }
        public List<ITrait> Traits { get; set; }

        public IdentityTraitsKey(string identity, List<ITrait> traits)
        {
            Identity = identity;
            Traits = traits;
        }

        public string GenerateUniqueKey()
        {
            var traitsJson = Newtonsoft.Json.JsonConvert.SerializeObject(Traits);

            var combinedString = Identity + traitsJson;

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));

                var stringBuilder = new StringBuilder();
                foreach (var t in hashBytes)
                {
                    stringBuilder.Append(t.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}