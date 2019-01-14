using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SolidStateGroup.BulletTrain
{
  [JsonObject(MemberSerialization.OptIn)]
  public class Identity
  {
    [JsonProperty]
    public List<Flag> flags;

    [JsonProperty]
    public List<Trait> traits;

    public override string ToString() {
      return JsonConvert.SerializeObject(this);
    }
  }
}
