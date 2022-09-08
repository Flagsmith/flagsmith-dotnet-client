using System.Collections.Generic;

namespace Flagsmith.Interfaces
{
    public interface IFlags
    {
        IReadOnlyCollection<IFlag> Flags { get; }

        string GetFeatureValue(string featureName);
        bool IsFeatureEnabled(string featureName);
        IFlag GetFlag(string featureName);
    }
}
