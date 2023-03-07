namespace Flagsmith
{
    public interface ISegment
    {
        int Id { get; set; }
        string Name { get; set; }
        string ToString();
    }
}