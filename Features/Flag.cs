using System.Text.Json;
using System.Text.Json.Serialization;

namespace BulletTrain
{
    public class Flag
    {
        [JsonPropertyName("feature")]
        public Feature Feature { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public Feature GetFeature()
        {
            return Feature;
        }

        public bool IsEnabled()
        {
            return Enabled;
        }

        public string GetValue()
        {
            return Value;
        }
    }
}
