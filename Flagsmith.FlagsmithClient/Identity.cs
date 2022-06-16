using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Identity
    {
        [JsonProperty]
        public List<Flag> flags;

        [JsonProperty]
        public Dictionary<string, object> traits;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
