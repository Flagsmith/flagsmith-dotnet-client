using System;
using Newtonsoft.Json;

namespace SolidStateGroup.BulletTrain
{
  [JsonObject(MemberSerialization.OptIn)]
  public class Flag
  {
    [JsonProperty]
    private Feature feature;

    [JsonProperty]
    private bool enabled;

    [JsonProperty("feature_state_value")]
    private string value;

    public override string ToString() {
      return JsonConvert.SerializeObject(this);
    }

    public Feature GetFeature() {
      return feature;
    }

    public bool IsEnabled() {
      return enabled;
    }

    public string GetValue() {
      return value;
    }
  }
}
