namespace Flagsmith.Interfaces
{
    public interface IFlag
    {
        int Id { get; }
        bool Enabled { get; }
        string Value { get; }
        IFeature Feature { get; }
    }
}
