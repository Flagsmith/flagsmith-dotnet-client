using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Trait
    {
        public Trait() { }
        public Trait(string traitKey, object traitValue)
        {
            this.traitKey = traitKey;
            this.traitValue = traitValue;
        }

        [JsonProperty("trait_key")]
        private string traitKey = null;

        [JsonProperty("trait_value")]
        private object traitValue = null;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string getTraitKey() 
        {
            return this.traitKey;
        }

        public object getTraitValue() 
        {
            return this.traitValue;
        }
    }
}
