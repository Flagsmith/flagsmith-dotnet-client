#nullable enable

using System;
using System.Collections.Generic;

namespace Flagsmith.Cache.Hybrid
{
    /// <summary>
    /// Compares the traits of an incoming request against the traits Flagsmith is known to hold, so
    /// that a cached flag list can be discarded when it can no longer be trusted.
    /// </summary>
    internal static class TraitFingerprint
    {
        /// <summary>
        /// Reduces traits to a trait key to value fingerprint map.
        /// </summary>
        internal static Dictionary<string, string> Create(List<ITrait>? traits)
        {
            var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
            if (traits == null)
            {
                return fingerprints;
            }

            foreach (var trait in traits)
            {
                var key = trait?.GetTraitKey();
                if (key == null)
                {
                    continue;
                }

                // ITrait.ToString() renders the key, the value and the transient flag, all of which
                // change what Flagsmith evaluates.
                fingerprints[key] = trait!.ToString();
            }

            return fingerprints;
        }

        /// <summary>
        /// Returns true when <paramref name="current"/> carries a trait that <paramref name="known"/>
        /// does not, or a different value for one it does.
        /// Traits missing from <paramref name="current"/> are ignored on purpose: Flagsmith keeps the
        /// traits it has already been told about, so omitting one cannot change the flags it returns.
        /// </summary>
        internal static bool HasAddedOrChangedTraits(
            IDictionary<string, string> known,
            IDictionary<string, string> current)
        {
            foreach (var trait in current)
            {
                if (!known.TryGetValue(trait.Key, out var knownFingerprint) ||
                    !string.Equals(knownFingerprint, trait.Value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Folds <paramref name="current"/> into <paramref name="known"/>. Traits are never dropped, so
        /// that a later request carrying only a subset of them is still recognised as unchanged.
        /// </summary>
        internal static Dictionary<string, string> Merge(
            IDictionary<string, string> known,
            IDictionary<string, string> current)
        {
            var merged = new Dictionary<string, string>(known, StringComparer.Ordinal);
            foreach (var trait in current)
            {
                merged[trait.Key] = trait.Value;
            }

            return merged;
        }
    }
}
