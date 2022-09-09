using Flagsmith.Interfaces;
using Newtonsoft.Json;

namespace Flagsmith
{
    public class Segment : ISegment
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Segment()
        {
        }

        public Segment(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}