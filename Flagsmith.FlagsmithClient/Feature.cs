using Flagsmith.Interfaces;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class Feature : IFeature
    {
        public Feature()
        {
        }

        public Feature(string name, int id = 0)
        {
            Id = id;
            Name = name;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
