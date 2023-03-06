using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith
{
    public interface IFlags
    {
        Task<string> GetFeatureValue(string featureName);
        Task<bool> IsFeatureEnabled(string featureName);
        Task<Flag> GetFlag(string featureName);
        List<Flag> AllFlags();
    }
}