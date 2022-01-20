using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Trait
    {
        [JsonProperty("trait_key")]
        private string key = null;

        [JsonProperty("string_value")]
        private string strValue = null;

        [JsonProperty("integer_value")]
        private int intValue = 0;

        [JsonProperty("bool_value")]
        private bool boolValue = false;

        // Support for legacy non-typed value (only support string)
        [JsonProperty("trait_value")]
        private string traitValue = null;

        public string GetKey()
        {
            return key;
        }

        public string GetValue()
        {
            if (traitValue != null) {
                return traitValue;
            }
            return strValue;
        }

        public string GetStringValue()
        {
            if (traitValue != null) {
                return traitValue;
            }
            return strValue;
        }

        public bool GetBoolValue()
        {
            return boolValue;
        }

        public int GetIntValue()
        {
            return intValue;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
