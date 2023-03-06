using Newtonsoft.Json;

namespace Flagsmith
{
    public class Feature : IFeature
    {
        public Feature(string name, int id = default)
        {
            this.Id = id;
            this.Name = name;
        }
        [JsonProperty("id")]
        public int Id { get; private set; }
        [JsonProperty("name")]
        public string Name { get; private set; }
    }
}
