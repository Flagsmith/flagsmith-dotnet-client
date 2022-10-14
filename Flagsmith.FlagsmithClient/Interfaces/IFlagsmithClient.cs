using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith.Interfaces
{
    /// <summary>
    /// A Flagsmith client interface.
    /// Provides a functionality for interacting with the Flagsmith http API.
    /// </summary>
    public interface IFlagsmithClient
    {
        /// <summary>
        /// Get all the default for flags for the current environment.
        /// </summary>
        Task<IFlags> GetEnvironmentFlags();

        /// <summary>
        /// Get all the flags for the current environment for a given identity.
        /// </summary>
        /// <param name="identity">identity</param>
        /// <returns></returns>
        Task<IFlags> GetIdentityFlags(string identity);

        /// <summary>
        /// Get all the flags for the current environment for a given identity with provided traits.
        /// </summary>
        /// <param name="identity">identity</param>
        /// <param name="traits">traits</param>
        /// <returns></returns>
        Task<IFlags> GetIdentityFlags(string identity, IEnumerable<ITrait> traits);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="traits"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<ISegment>> GetIdentitySegments(string identifier, IEnumerable<ITrait> traits);
    }
}
