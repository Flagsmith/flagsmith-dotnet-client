using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith
{
    public interface IFlagsmithClient
    {
        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        Task<IFlags> GetEnvironmentFlags();

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        Task<IFlags> GetIdentityFlags(string identifier);

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        Task<IFlags> GetIdentityFlags(string identifier, List<ITrait> traits, bool transient = false);

        List<ISegment> GetIdentitySegments(string identifier);
        List<ISegment> GetIdentitySegments(string identifier, List<ITrait> traits);
    }
}