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

        /// <summary>
        /// Get an identity's flag for a feature, and record which variant the identity was exposed to
        /// when it is enrolled in a running experiment on the feature.
        /// </summary>
        Task<IFlag> GetExperimentFlag(string featureName, string identifier, List<ITrait> traits = null);

        /// <summary>
        /// Record an application event, such as a conversion, for experimentation.
        /// </summary>
        void TrackEvent(string eventName, string identifier = null, object value = null, List<ITrait> traits = null, Dictionary<string, object> metadata = null);

        /// <summary>
        /// Record that an identity was exposed to a variant of a feature, as a "$flag_exposure" event.
        /// </summary>
        void TrackExposureEvent(string featureName, string identifier, object value = null, List<ITrait> traits = null, Dictionary<string, object> metadata = null);

        /// <summary>
        /// Send the buffered events now.
        /// </summary>
        Task FlushEvents();

        List<ISegment> GetIdentitySegments(string identifier);
        List<ISegment> GetIdentitySegments(string identifier, List<ITrait> traits);
    }
}