using Flagsmith.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Identity : IIdentity
    {
        [JsonProperty]
        public List<Flag> flags;

        [JsonProperty]
        public List<Trait> traits;

        IReadOnlyCollection<IFlag> IIdentity.Flags => flags;

        IReadOnlyCollection<ITrait> IIdentity.Traits => traits;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
