using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Identity : IIdentity
    {
        [JsonProperty]
        public List<Flag> flags;

        [JsonProperty]
        public List<Trait> traits;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
