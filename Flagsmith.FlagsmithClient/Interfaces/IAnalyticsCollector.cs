namespace Flagsmith.Interfaces
{
    public interface IAnalyticsCollector
    {
        void TrackFeature(string name);
    }
}
