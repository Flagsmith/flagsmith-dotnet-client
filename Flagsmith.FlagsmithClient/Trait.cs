using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Trait : ITrait
    {
        public Trait() { }
        public Trait(string traitKey, dynamic traitValue)
        {
            this.traitKey = traitKey;
            this.traitValue = traitValue;
        }

        [JsonProperty("trait_key")]
        private string traitKey = null;

        [JsonProperty("trait_value")]
        private dynamic traitValue = null;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetTraitKey()
        {
            return this.traitKey;
        }

        public dynamic GetTraitValue()
        {
            return this.traitValue;
        }
    }
}
