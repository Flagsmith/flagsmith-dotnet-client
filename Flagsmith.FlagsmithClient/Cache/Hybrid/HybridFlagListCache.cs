#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

namespace Flagsmith.Cache.Hybrid
{
    /// <summary>
    /// Stores flag lists in a <see cref="HybridCache"/> supplied by the host application.
    /// </summary>
    internal class HybridFlagListCache
    {
        private readonly HybridCache _cache;
        private readonly HybridCacheConfig _config;
        private readonly HybridCacheEntryOptions _entryOptions;
        private readonly Func<List<IFlag>, IFlags> _flagsFactory;
        private readonly string _keyPrefix;

        internal HybridFlagListCache(
            HybridCacheConfig config,
            string? environmentKey,
            Func<List<IFlag>, IFlags> flagsFactory)
        {
            _config = config;
            _cache = config.Cache!;
            _flagsFactory = flagsFactory;
            _entryOptions = new HybridCacheEntryOptions
            {
                Expiration = config.Duration,
                LocalCacheExpiration = config.LocalCacheDuration ?? config.Duration
            };

            // Scope keys by environment so several clients can share one HybridCache instance.
            _keyPrefix = $"{config.KeyPrefix}:{Utils.GetHashString(environmentKey ?? string.Empty).Substring(0, 16)}";
        }

        internal async Task<IFlags> GetEnvironmentFlags(Func<Task<IFlags>> getFlags)
        {
            var json = await _cache.GetOrCreateAsync(
                $"{_keyPrefix}:environment",
                getFlags,
                async (factory, _) => Serialize(
                    await factory().ConfigureAwait(false),
                    new Dictionary<string, string>()),
                _entryOptions).ConfigureAwait(false);

            return ToFlags(Deserialize(json));
        }

        internal async Task<IFlags> GetIdentityFlags(
            IdentityWrapper identityWrapper,
            Func<IdentityWrapper, Task<IFlags>> getFlags)
        {
            var key = IdentityKey(identityWrapper);
            var currentTraits = TraitFingerprint.Create(identityWrapper.Traits);

            var json = await _cache.GetOrCreateAsync(
                key,
                (identityWrapper, currentTraits, getFlags),
                async (state, _) => Serialize(
                    await state.getFlags(state.identityWrapper).ConfigureAwait(false),
                    state.currentTraits),
                _entryOptions).ConfigureAwait(false);

            var entry = Deserialize(json);

            if (!_config.RefreshOnTraitChanges ||
                !TraitFingerprint.HasAddedOrChangedTraits(entry.Traits, currentTraits))
            {
                return ToFlags(entry);
            }

            // Flagsmith only learns of a trait once we send it, so an added or changed trait has to
            // reach the API before the flags derived from it can be trusted. Callers racing here cost
            // an extra request, never a stale result.
            var flags = await getFlags(identityWrapper).ConfigureAwait(false);
            await _cache.SetAsync(
                key,
                Serialize(flags, TraitFingerprint.Merge(entry.Traits, currentTraits)),
                _entryOptions).ConfigureAwait(false);

            return flags;
        }

        private string IdentityKey(IdentityWrapper identityWrapper)
        {
            // A transient identity is not persisted by Flagsmith and so evaluates differently.
            var scope = identityWrapper.Transient ? "transient-identity" : "identity";
            return $"{_keyPrefix}:{scope}:{Utils.GetHashString(identityWrapper.Identifier ?? string.Empty)}";
        }

        private static string Serialize(IFlags flags, Dictionary<string, string> traits)
        {
            return CachedFlagListSerializer.Serialize(new CachedFlagList
            {
                Flags = flags.AllFlags()?.Select(ToCachedFlag).ToList() ?? new List<CachedFlag>(),
                Traits = traits
            });
        }

        private static CachedFlagList Deserialize(string json)
        {
            return CachedFlagListSerializer.Deserialize(json);
        }

        private static CachedFlag ToCachedFlag(IFlag flag)
        {
            return new CachedFlag
            {
                FeatureName = flag.GetFeatureName(),
                FeatureId = (flag as Flag)?.GetFeatureId() ?? default,
                Enabled = flag.Enabled,
                Value = flag.Value
            };
        }

        private static IFlag FromCachedFlag(CachedFlag flag)
        {
            return new Flag(new Feature(flag.FeatureName, flag.FeatureId), flag.Enabled, flag.Value);
        }

        private IFlags ToFlags(CachedFlagList entry)
        {
            return _flagsFactory(entry.Flags.Select(FromCachedFlag).ToList());
        }
    }
}
