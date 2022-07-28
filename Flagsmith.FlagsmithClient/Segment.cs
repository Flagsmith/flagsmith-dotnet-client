using Newtonsoft.Json;

namespace Flagsmith
{
    public class Segment
    {
        public Segment(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}