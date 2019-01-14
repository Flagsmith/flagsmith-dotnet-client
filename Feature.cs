using System;
using Newtonsoft.Json;

namespace SolidStateGroup.BulletTrain
{
  [JsonObject(MemberSerialization.OptIn)]
  public class Feature
  {
    [JsonProperty]
    private string name;

    public string GetName() {
      return name;
    }

    public override string ToString() {
      return JsonConvert.SerializeObject(this);
    }
  }
}
