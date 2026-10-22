#nullable enable

using System;
using Microsoft.Extensions.Caching.Hybrid;

namespace Flagsmith
{
    /// <summary>
    /// Caches flags in a <see cref="HybridCache"/> owned by the host application, so that flags can
    /// be shared across instances through the distributed (L2) cache the host has configured.
    /// This is an alternative to <see cref="CacheConfig"/>; only one of the two may be enabled.
    /// </summary>
    public class HybridCacheConfig
    {
        /// <summary>
        /// Creates a disabled configuration.
        /// </summary>
        public HybridCacheConfig()
        {
        }

        /// <summary>
        /// Creates a configuration backed by the given cache.
        /// </summary>
        /// <param name="cache">
        /// The cache to store flags in, typically resolved from the service provider after calling
        /// <c>AddHybridCache()</c>.
        /// </param>
        /// <param name="enabled">Whether caching is enabled.</param>
        public HybridCacheConfig(HybridCache cache, bool enabled = true)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            Enabled = enabled;
        }

        /// <summary>
        /// Whether flags are cached in <see cref="Cache"/>.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The cache to store flags in. Required when <see cref="Enabled"/> is set.
        /// </summary>
        public HybridCache? Cache { get; set; }

        /// <summary>
        /// How long a cached flag list is served before it is fetched again. Defaults to 5 minutes.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How long a flag list is kept in the in-process (L1) cache. Defaults to <see cref="Duration"/>.
        /// Set this lower than <see cref="Duration"/> to let instances pick up each other's writes sooner.
        /// </summary>
        public TimeSpan? LocalCacheDuration { get; set; }

        /// <summary>
        /// Prefix for every cache key written by the SDK. Keys are further scoped by environment key,
        /// so several clients may share one <see cref="HybridCache"/>.
        /// </summary>
        public string KeyPrefix { get; set; } = "flagsmith";

        /// <summary>
        /// When enabled, cached identity flags are discarded and fetched again as soon as a request
        /// carries a trait Flagsmith has not been told about, or a new value for one it has. This
        /// guarantees such trait changes reach Flagsmith instead of waiting out <see cref="Duration"/>.
        /// Traits that are merely absent from a request do not trigger a refresh: Flagsmith keeps the
        /// traits it already holds, so omitting one cannot change the flags it returns.
        /// Disabled by default, which keeps evaluation stateless and consistent with the other
        /// server-side SDKs: cached flags are then only ever refreshed once <see cref="Duration"/>
        /// has elapsed.
        /// </summary>
        public bool RefreshOnTraitChanges { get; set; }
    }
}
