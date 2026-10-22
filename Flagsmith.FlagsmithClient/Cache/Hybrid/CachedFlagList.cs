#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;
#if NET9_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace Flagsmith.Cache.Hybrid
{
    /// <summary>
    /// What the SDK stores for one cache entry: the flag list, plus the identity traits Flagsmith was
    /// known to hold when that list was fetched.
    /// Property names are spelled out rather than left to a naming policy, because the same entry may
    /// be written by one target framework and read back by another through a shared L2 cache.
    /// </summary>
    internal class CachedFlagList
    {
        [JsonProperty("flags")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("flags")]
#endif
        public List<CachedFlag> Flags { get; set; } = new List<CachedFlag>();

        /// <summary>
        /// Trait key to a fingerprint of the trait as it was last sent to Flagsmith.
        /// </summary>
        [JsonProperty("traits")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("traits")]
#endif
        public Dictionary<string, string> Traits { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// A single flag, reduced to the state that survives a round trip through the cache.
    /// </summary>
    internal class CachedFlag
    {
        [JsonProperty("feature_name")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("feature_name")]
#endif
        public string FeatureName { get; set; } = string.Empty;

        [JsonProperty("feature_id")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("feature_id")]
#endif
        public int FeatureId { get; set; }

        [JsonProperty("enabled")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("enabled")]
#endif
        public bool Enabled { get; set; }

        [JsonProperty("value")]
#if NET9_0_OR_GREATER
        [JsonPropertyName("value")]
#endif
        public string? Value { get; set; }
    }
}
