namespace Flagsmith
{
    public interface IFlag
    {
        bool Enabled { get; }
        string Value { get; }
        string GetFeatureName();
    }
}