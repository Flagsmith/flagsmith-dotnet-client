using Newtonsoft.Json;

namespace Flagsmith
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Feature
    {
        public Feature() { }
        public Feature(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        [JsonProperty("id")]
        private int Id;
        [JsonProperty("name")]
        private string Name = null;

        public string GetName()
        {
            return Name;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
