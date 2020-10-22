using System.Collections.Generic;
using System.Threading.Tasks;

namespace BulletTrain
{
    public interface IBulletTrainClient
    {
        /// <summary>
        /// Get all feature flags (flags and remote config) optionally for a specific identity.
        /// </summary>
        Task<List<Flag>> GetFeatureFlags(string identity = null);

        /// <summary>
        /// Check feature exists and is enabled optionally for a specific identity
        /// </summary>
        Task<bool> HasFeatureFlag(string featureId, string identity = null);

        /// <summary>
        /// Get remote config value optionally for a specific identity
        /// </summary>
        Task<string> GetFeatureValue(string featureId, string identity = null);

        /// <summary>
        /// Get all user traits for provided identity. Optionally filter results with a list of keys
        /// </summary>
        Task<List<Trait>> GetTraits(string identity, List<string> keys = null);

        /// <summary>
        /// Get user trait for provided identity and trait key.
        /// </summary>
        Task<string> GetTrait(string identity, string key);

        /// <summary>
        /// Get boolean user trait for provided identity and trait key.
        /// </summary>
        Task<bool> GetBoolTrait(string identity, string key);

        /// <summary>
        /// Get integer user trait for provided identity and trait key.
        /// </summary>
        Task<int> GetIntegerTrait(string identity, string key);

        /// <summary>
        /// Set user trait value for provided identity and trait key.
        /// </summary>
        Task<Trait> SetTrait(string identity, string key, object value);

        /// <summary>
        /// Increment user trait value for provided identity and trait key.
        /// </summary>
        Task<Trait> IncrementTrait(string identity, string key, int incrementBy);

        /// <summary>
        /// Get both feature flags and user traits for the provided identity
        /// </summary>
        Task<Identity> GetUserIdentity(string identity);
    }
}