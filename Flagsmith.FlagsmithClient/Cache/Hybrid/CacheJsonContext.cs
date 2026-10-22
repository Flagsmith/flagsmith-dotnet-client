#if NET9_0_OR_GREATER
using System.Text.Json.Serialization;

namespace Flagsmith.Cache.Hybrid
{
    /// <summary>
    /// Source-generated serialization metadata for cache entries, so that reading and writing them
    /// needs no runtime reflection and stays trimming and AOT friendly.
    /// </summary>
    [JsonSerializable(typeof(CachedFlagList))]
    internal partial class CacheJsonContext : JsonSerializerContext
    {
    }
}
#endif
