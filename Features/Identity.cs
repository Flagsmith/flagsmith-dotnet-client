using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BulletTrain
{
    public class Identity
    {
        [JsonPropertyName("flags")]
        public List<Flag> Flags { get; set; }

        [JsonPropertyName("traits")]
        public List<Trait> Traits { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
