using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith.Interfaces
{
    public interface IFlags
    {
        Task<string> GetFeatureValue(string featureName);
        Task<bool> IsFeatureEnabled(string featureName);
        Task<IFlag> GetFlag(string featureName);
        Task<IReadOnlyCollection<IFlag>> AllFlags();
    }
}
