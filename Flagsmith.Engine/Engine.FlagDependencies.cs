using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FlagsmithEngine
{
    public partial class Engine
    {
        private const string FlagsPathPrefix = "$.flags";

        /// <summary>
        /// The `$.flags` of an evaluation context, resolving a flag the first time a segment condition reads it.
        /// Flags supplied with the context are discarded: `$.flags` only ever holds flags resolved here.
        /// </summary>
        private sealed class FlagDependencies<SegmentMetadataT, FeatureMetadataT>
        {
            private readonly EvaluationContext<SegmentMetadataT, FeatureMetadataT> _context;
            private readonly Dictionary<string, FlagResult<FeatureMetadataT>> _resolved = new Dictionary<string, FlagResult<FeatureMetadataT>>();
            private readonly HashSet<string> _cyclic = new HashSet<string>();
            private readonly List<string> _resolving = new List<string>();
            private readonly Dictionary<string, bool> _segmentMatches = new Dictionary<string, bool>();
            private Dictionary<string, List<string>> _segmentKeysByFeatureName;

            public FlagDependencies(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
            {
                _context = context;
            }

            /// <summary>
            /// Whether a segment condition read any flag.
            /// </summary>
            public bool Used { get; private set; }

            /// <summary>
            /// Incremented whenever a dependency cycle is cut. A segment whose evaluation increments it
            /// read a flag that could not be resolved, so it must not match.
            /// </summary>
            public int CycleHits { get; private set; }

            /// <summary>
            /// Get the flag result of a feature, or null for a feature absent from the context or in a dependency cycle.
            /// </summary>
            public FlagResult<FeatureMetadataT> Get(string featureName)
            {
                Used = true;
                if (!_resolved.ContainsKey(featureName))
                    Resolve(featureName);
                _resolved.TryGetValue(featureName, out var flag);
                return flag;
            }

            /// <summary>
            /// Apply the flags resolved for segment conditions over the single-pass flag results.
            /// </summary>
            public void ApplyTo(Dictionary<string, FlagResult<FeatureMetadataT>> flags)
            {
                foreach (var resolvedFlag in _resolved)
                    flags[resolvedFlag.Key] = resolvedFlag.Value;
                foreach (var featureName in _cyclic)
                    if (flags.TryGetValue(featureName, out var flag))
                        flag.Reason = CircularDependencyReason;
            }

            private void Resolve(string featureName)
            {
                var cycleStart = _resolving.IndexOf(featureName);
                if (cycleStart >= 0)
                {
                    _cyclic.UnionWith(_resolving.GetRange(cycleStart, _resolving.Count - cycleStart));
                    CycleHits++;
                    return;
                }

                FeatureContext<FeatureMetadataT> featureContext = null;
                if (!(_context.Features?.TryGetValue(featureName, out featureContext) ?? false))
                    return;

                _resolving.Add(featureName);
                SegmentOverride<FeatureMetadataT> segmentOverride;
                try
                {
                    segmentOverride = GetSegmentOverride(featureName);
                }
                finally
                {
                    _resolving.RemoveAt(_resolving.Count - 1);
                }

                if (_cyclic.Contains(featureName))
                    return;

                _resolved[featureName] = GetFeatureFlagResult(
                    _context,
                    featureContext,
                    segmentOverride,
                    SegmentKeysByFeatureName.ContainsKey(featureName)
                );
            }

            private SegmentOverride<FeatureMetadataT> GetSegmentOverride(string featureName)
            {
                SegmentOverride<FeatureMetadataT> segmentOverride = null;
                if (!SegmentKeysByFeatureName.TryGetValue(featureName, out var segmentKeys))
                    return null;

                foreach (var segmentKey in segmentKeys)
                {
                    if (!MatchesSegment(segmentKey))
                        continue;
                    var segmentContext = _context.Segments[segmentKey];
                    foreach (var overrideFeatureContext in segmentContext.Overrides)
                    {
                        if (overrideFeatureContext.Name != featureName || !WinsOver(overrideFeatureContext, segmentOverride?.FeatureContext))
                            continue;
                        segmentOverride = new SegmentOverride<FeatureMetadataT>
                        {
                            FeatureContext = overrideFeatureContext,
                            SegmentName = segmentContext.Name
                        };
                    }
                }

                return segmentOverride;
            }

            private bool MatchesSegment(string segmentKey)
            {
                if (_segmentMatches.TryGetValue(segmentKey, out var memoised))
                    return memoised;

                var cycleHits = CycleHits;
                var matches = IsContextInSegment(_context, _context.Segments[segmentKey], this);
                if (CycleHits != cycleHits)
                    return false;

                _segmentMatches[segmentKey] = matches;
                return matches;
            }

            private Dictionary<string, List<string>> SegmentKeysByFeatureName
            {
                get
                {
                    if (_segmentKeysByFeatureName != null)
                        return _segmentKeysByFeatureName;

                    _segmentKeysByFeatureName = new Dictionary<string, List<string>>();
                    if (_context.Segments is null)
                        return _segmentKeysByFeatureName;

                    foreach (var segmentItem in _context.Segments)
                    {
                        if (segmentItem.Value.Overrides is null)
                            continue;
                        foreach (var overrideFeatureContext in segmentItem.Value.Overrides)
                        {
                            if (!_segmentKeysByFeatureName.TryGetValue(overrideFeatureContext.Name, out var segmentKeys))
                                _segmentKeysByFeatureName[overrideFeatureContext.Name] = segmentKeys = new List<string>();
                            if (!segmentKeys.Contains(segmentItem.Key))
                                segmentKeys.Add(segmentItem.Key);
                        }
                    }
                    return _segmentKeysByFeatureName;
                }
            }
        }

        /// <summary>
        /// Get the feature name a JSONPath property reads from `$.flags`, e.g. `my_feature` from `$.flags.my_feature.enabled` or `$.flags['my_feature'].enabled`.
        /// </summary>
        private static bool TryGetFlagDependencyName(string property, out string featureName)
        {
            featureName = null;
            if (!property.StartsWith(FlagsPathPrefix) || property.Length <= FlagsPathPrefix.Length)
                return false;

            var position = FlagsPathPrefix.Length;
            switch (property[position])
            {
                case '.':
                    var nameStart = position + 1;
                    var nameEnd = property.IndexOfAny(new[] { '.', '[' }, nameStart);
                    if (nameEnd < 0)
                        nameEnd = property.Length;
                    featureName = property.Substring(nameStart, nameEnd - nameStart);
                    return featureName.Length > 0 && featureName != "*";
                case '[':
                    if (property.Length <= position + 1)
                        return false;
                    var quote = property[position + 1];
                    if (quote != '\'' && quote != '"')
                        return false;
                    var name = new StringBuilder();
                    for (var index = position + 2; index < property.Length; index++)
                    {
                        var character = property[index];
                        if (character == '\\' && index + 1 < property.Length)
                            name.Append(property[++index]);
                        else if (character == quote)
                        {
                            featureName = name.ToString();
                            return true;
                        }
                        else
                            name.Append(character);
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static JObject GetFlagsRoot<_, __>(string property, FlagDependencies<_, __> flagDependencies)
        {
            var flags = new JObject();
            if (TryGetFlagDependencyName(property, out var featureName))
            {
                var flag = flagDependencies.Get(featureName);
                if (flag != null)
                    flags[featureName] = JToken.FromObject(flag);
            }
            return new JObject { ["flags"] = flags };
        }
    }
}
