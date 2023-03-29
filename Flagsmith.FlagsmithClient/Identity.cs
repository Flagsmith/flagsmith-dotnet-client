using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Identity : IIdentity
    {
        [JsonProperty]
        public List<IFlag> flags;

        [JsonProperty]
        public List<ITrait> traits;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
