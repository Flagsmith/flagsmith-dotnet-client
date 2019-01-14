using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Trait
{
  [JsonProperty("trait_key")]
  private string key;

  [JsonProperty("trait_value")]
  private string value;

  public string GetKey()
  {
    return key;
  }

  public string GetValue()
  {
    return value;
  }

  public override string ToString() {
    return JsonConvert.SerializeObject(this);
  }
}
