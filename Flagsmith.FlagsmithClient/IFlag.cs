namespace Flagsmith
{
    public interface IFlag
    {
        int Id { get; }
        bool Enabled { get; }
        string Value { get; }
        int getFeatureId();
        string GetFeatureName();
        string ToString();
    }
}