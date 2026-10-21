using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FlagsmithEngine
{
    public partial class Engine
    {
        private const string FlagsPathPrefix = "$.flags";

        /// <summary>
        /// The `$.flags` mapping read by segment conditions, resolving each flag the first time a condition reads it.
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
            /// Whether any segment condition has read a flag.
            /// </summary>
            public bool Used { get; private set; }

            /// <summary>
            /// Incremented whenever a dependency cycle is cut. A segment whose evaluation increments it read a flag that could not be resolved.
            /// </summary>
            public int CycleHits { get; private set; }

            /// <summary>
            /// Flags resolved for segment conditions, mapped by feature name. Flags in a dependency cycle are never resolved.
            /// </summary>
            public IReadOnlyDictionary<string, FlagResult<FeatureMetadataT>> Resolved => _resolved;

            /// <summary>
            /// Names of the features found to be in a dependency cycle.
            /// </summary>
            public IEnumerable<string> Cyclic => _cyclic;

            /// <summary>
            /// Get the flag result for a feature, or null when the feature is absent from the context or in a dependency cycle.
            /// </summary>
            public FlagResult<FeatureMetadataT> Get(string featureName)
            {
                if (!_resolved.TryGetValue(featureName, out var flag))
                {
                    Used = true;
                    Resolve(featureName);
                    _resolved.TryGetValue(featureName, out flag);
                }
                return flag;
            }

            private void Resolve(string featureName)
            {
                var resolvingIndex = _resolving.IndexOf(featureName);
                if (resolvingIndex >= 0)
                {
                    _cyclic.UnionWith(_resolving.GetRange(resolvingIndex, _resolving.Count - resolvingIndex));
                    CycleHits++;
                    return;
                }

                FeatureContext<FeatureMetadataT> featureContext = null;
                if (!(_context.Features?.TryGetValue(featureName, out featureContext) ?? false))
                    return;

                _resolving.Add(featureName);
                var segmentOverride = GetSegmentOverride(featureName);
                _resolving.RemoveAt(_resolving.Count - 1);

                if (_cyclic.Contains(featureName))
                    return;

                _resolved[featureName] = segmentOverride is null
                    ? GetFlagResult(_context, featureContext, "DEFAULT")
                    : GetFlagResult(_context, segmentOverride.FeatureContext, GetTargetingMatchReason(segmentOverride.SegmentName));
            }

            private SegmentOverride<FeatureMetadataT> GetSegmentOverride(string featureName)
            {
                SegmentOverride<FeatureMetadataT> segmentOverride = null;

                if (!GetSegmentKeysByFeatureName().TryGetValue(featureName, out var segmentKeys))
                    return segmentOverride;

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
                if (_segmentMatches.TryGetValue(segmentKey, out var matches))
                    return matches;

                var cycleHits = CycleHits;
                matches = IsContextInSegment(_context, _context.Segments[segmentKey], this);

                if (CycleHits != cycleHits)
                    return false; // Rests on a flag left unresolved by a dependency cycle

                _segmentMatches[segmentKey] = matches;
                return matches;
            }

            private Dictionary<string, List<string>> GetSegmentKeysByFeatureName()
            {
                if (_segmentKeysByFeatureName != null)
                    return _segmentKeysByFeatureName;

                _segmentKeysByFeatureName = new Dictionary<string, List<string>>();
                foreach (var segmentItem in _context.Segments ?? new Dictionary<string, SegmentContext<SegmentMetadataT, FeatureMetadataT>>())
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

        private static JObject GetFlagsRoot<FeatureMetadataT>(string featureName, FlagResult<FeatureMetadataT> flag)
        {
            var flags = new JObject();
            if (flag != null)
                flags[featureName] = JToken.FromObject(flag);
            return new JObject { ["flags"] = flags };
        }
    }
}
