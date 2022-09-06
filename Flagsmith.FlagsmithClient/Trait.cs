using Flagsmith.Interfaces;
using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Trait : ITrait
    {
        [JsonProperty("trait_key")]
        public string Key { get; }

        [JsonProperty("trait_value")]
        public dynamic Value { get; }

        public Trait()
        {
        }

        public Trait(string traitKey, dynamic traitValue)
        {
            Key = traitKey;
            Value = traitValue;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetTraitKey() => Key;

        public dynamic GetTraitValue() => Value;
    }
}
