using System.Text.Json;

namespace BulletTrain
{
    public class Feature
    {
        public string Name {get;set; }

        public string GetName()
        {
            return Name;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
