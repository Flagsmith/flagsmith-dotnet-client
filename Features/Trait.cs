using System.Text.Json;
using System.Text.Json.Serialization;

namespace BulletTrain
{
    public class Trait
    {
        [JsonPropertyName("trait_key")]
        public string Key {get;set;}

        [JsonPropertyName("string_value")]
        public string StrValue {get;set;}

        [JsonPropertyName("integer_value")]
        public int IntValue { get; set; }

        [JsonPropertyName("bool_value")]
        public bool BoolValue { get; set; }

        public string GetKey()
        {
            return Key;
        }

        public string GetValue()
        {
            return StrValue;
        }

        public string GetStringValue()
        {
            return StrValue;
        }

        public bool GetBoolValue()
        {
            return BoolValue;
        }

        public int GetIntValue()
        {
            return IntValue;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
