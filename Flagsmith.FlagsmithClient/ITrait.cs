namespace Flagsmith
{
    public interface ITrait
    {
        string ToString();
        string GetTraitKey();
        dynamic GetTraitValue();
    }
}