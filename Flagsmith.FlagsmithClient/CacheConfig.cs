namespace Flagsmith
{
    public class CacheConfig
    {
        public bool Enabled { get; set; }
        public int DurationInMinutes { get; set; }
        
        public CacheConfig(bool enabled)
        {
            Enabled = enabled;
            DurationInMinutes = 5;
        }
    }
}