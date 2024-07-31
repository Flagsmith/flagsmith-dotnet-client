using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Trait : ITrait
    {
        public Trait() { }
        public Trait(string traitKey, dynamic traitValue, bool transient = false)
        {
            this.traitKey = traitKey;
            this.traitValue = traitValue;
            this.transient = transient;
        }

        [JsonProperty("trait_key")]
        private string traitKey = null;

        [JsonProperty("trait_value")]
        private dynamic traitValue = null;

        [JsonProperty("transient")]
        private bool transient = false;


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
        public bool GetTransient()
        {
            return this.transient;
        }
    }
}
