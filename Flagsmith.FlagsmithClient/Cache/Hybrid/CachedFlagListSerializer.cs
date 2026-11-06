#nullable enable

#if NET9_0_OR_GREATER
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

namespace Flagsmith.Cache.Hybrid
{
    /// <summary>
    /// Reads and writes cache entries as JSON, so that any <c>HybridCache</c> backing store can hold
    /// them without the host having to register a serializer for SDK types.
    /// On .NET 9 and later this goes through the System.Text.Json source generator; older targets fall
    /// back to Newtonsoft.Json. Both produce the same JSON.
    /// </summary>
    internal static class CachedFlagListSerializer
    {
        internal static string Serialize(CachedFlagList entry)
        {
#if NET9_0_OR_GREATER
            return JsonSerializer.Serialize(entry, CacheJsonContext.Default.CachedFlagList);
#else
            return JsonConvert.SerializeObject(entry);
#endif
        }

        internal static CachedFlagList Deserialize(string json)
        {
#if NET9_0_OR_GREATER
            return JsonSerializer.Deserialize(json, CacheJsonContext.Default.CachedFlagList) ?? new CachedFlagList();
#else
            return JsonConvert.DeserializeObject<CachedFlagList>(json) ?? new CachedFlagList();
#endif
        }
    }
}
