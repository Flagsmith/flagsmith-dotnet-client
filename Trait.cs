using Newtonsoft.Json;

namespace BulletTrain
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

        public string GetKey()
        {
            return key;
        }

        public string GetValue()
        {
            return strValue;
        }

        public string GetStringValue()
        {
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
