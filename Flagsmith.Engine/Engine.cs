using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;

namespace FlagsmithEngine
{
    public class SegmentOverride<FeatureMetadataT>
    {
        public FeatureContext<FeatureMetadataT> FeatureContext { get; set; }
        public string SegmentName { get; set; }
    }

    public partial class Engine : IEngine
    {
        public static Hashing Hashing = new Hashing();

        public const double StrongestPriority = double.NegativeInfinity;
        public const double WeakestPriority = double.PositiveInfinity;

        public const string CircularDependencyReason = "ERROR; code=CIRCULAR_DEPENDENCY";

        private const string ControlVariant = "control";

        /// <summary>
        /// Get the evaluation result for a given context
        /// </summary>
        /// <typeparam name="SegmentMetadataT">Segment metadata type</typeparam>
        /// <typeparam name="FeatureMetadataT">Feature metadata type</typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public EvaluationResult<SegmentMetadataT, FeatureMetadataT> GetEvaluationResult<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
        {
            context = GetEnrichedEvaluationContext(context);
            var result = new EvaluationResult<SegmentMetadataT, FeatureMetadataT>();
            var flagDependencies = new FlagDependencies<SegmentMetadataT, FeatureMetadataT>(context);
            var segmentEvaluationResult = EvaluateSegments(context, flagDependencies);
            result.Flags = EvaluateFlags(context, segmentEvaluationResult.SegmentOverrides);
            result.Segments = segmentEvaluationResult.Segments;

            if (flagDependencies.Used)
            {
                foreach (var resolvedFlag in flagDependencies.Resolved)
                    result.Flags[resolvedFlag.Key] = resolvedFlag.Value;
                foreach (var featureName in flagDependencies.Cyclic)
                    if (result.Flags.TryGetValue(featureName, out var cyclicFlag))
                        cyclicFlag.Reason = CircularDependencyReason;
            }

            return result;
        }

        private EvaluationContext<SegmentMetadataT, FeatureMetadataT> GetEnrichedEvaluationContext<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
        {
            if (context.Identity != null)
            {
                if (string.IsNullOrEmpty(context.Identity.Key))
                {
                    context = context.Clone();
                    context.Identity.Key = context.Environment.Key + "_" + context.Identity.Identifier;
                }
            }
            return context;
        }

        private static (SegmentResult<SegmentMetadataT>[] Segments, Dictionary<string, SegmentOverride<FeatureMetadataT>> SegmentOverrides) EvaluateSegments<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context, FlagDependencies<SegmentMetadataT, FeatureMetadataT> flagDependencies)
        {
            var segmentOverrides = new Dictionary<string, SegmentOverride<FeatureMetadataT>>();

            if (context?.Segments is null)
            { return (Array.Empty<SegmentResult<SegmentMetadataT>>(), segmentOverrides); }

            var segmentResults = new List<SegmentResult<SegmentMetadataT>>();
            var cycleHits = flagDependencies.CycleHits;

            foreach (var segmentItem in context.Segments)
            {
                var segmentContext = segmentItem.Value;

                var matches = IsContextInSegment(context, segmentContext, flagDependencies);

                if (flagDependencies.CycleHits != cycleHits)
                {
                    // A segment read a flag left unresolved by a dependency cycle, so neither verdict is sound
                    cycleHits = flagDependencies.CycleHits;
                    continue;
                }

                if (!matches)
                    continue;

                segmentResults.Add(new SegmentResult<SegmentMetadataT>
                {
                    Name = segmentContext.Name,
                    Metadata = segmentContext.Metadata
                });

                if (segmentContext.Overrides is null)
                    continue;

                foreach (var segmentOverride in segmentContext.Overrides)
                {
                    segmentOverrides.TryGetValue(segmentOverride.Name, out var incumbent);
                    if (!WinsOver(segmentOverride, incumbent?.FeatureContext))
                        continue;
                    segmentOverrides[segmentOverride.Name] = new SegmentOverride<FeatureMetadataT>
                    {
                        FeatureContext = segmentOverride,
                        SegmentName = segmentContext.Name
                    };
                }
            }

            return (segmentResults.ToArray(), segmentOverrides);
        }

        /// <summary>
        /// Whether a segment override takes precedence over the one held so far: the lowest priority wins, and the latest seen wins a tie.
        /// </summary>
        private static bool WinsOver<FeatureMetadataT>(FeatureContext<FeatureMetadataT> candidate, FeatureContext<FeatureMetadataT> incumbent)
        {
            return incumbent is null || (candidate.Priority ?? WeakestPriority) <= (incumbent.Priority ?? WeakestPriority);
        }

        private static Dictionary<string, FlagResult<FeatureMetadataT>> EvaluateFlags<Any, FeatureMetadataT>(EvaluationContext<Any, FeatureMetadataT> context, Dictionary<string, SegmentOverride<FeatureMetadataT>> segmentOverrides)
        {
            var flags = new Dictionary<string, FlagResult<FeatureMetadataT>>();

            if (context?.Features is null)
                return flags;

            foreach (var featureItem in context.Features)
            {
                var featureContext = featureItem.Value;
                var featureName = featureContext.Name;
                if (segmentOverrides.ContainsKey(featureName))
                {
                    var segmentOverride = segmentOverrides[featureName];
                    flags[featureName] = GetFlagResult(
                        context,
                        segmentOverride.FeatureContext,
                        GetTargetingMatchReason(segmentOverride.SegmentName)
                    );
                }
                else
                {
                    flags[featureName] = GetFlagResult(
                        context,
                        featureContext,
                        "DEFAULT"
                    );
                }
            }

            return flags;
        }

        private static string GetTargetingMatchReason(string segmentName) => $"TARGETING_MATCH; segment={segmentName}";

        private static bool IsContextInSegment<_, __>(EvaluationContext<_, __> context, SegmentContext<_, __> segmentContext, FlagDependencies<_, __> flagDependencies)
        {
            return (
                segmentContext?.Rules != null &&
                segmentContext.Rules.All(rule => ContextMatchesRule(
                    context,
                    rule,
                    segmentContext.Key,
                    flagDependencies
                ))
            );
        }

        private static bool ContextMatchesRule<_, __>(EvaluationContext<_, __> context, SegmentRule rule, string segmentKey, FlagDependencies<_, __> flagDependencies)
        {
            bool matchesConditions;

            if (rule?.Conditions is null || !rule.Conditions.Any())
                // Sometimes rules are just groupers of subrules, having no intrinsic conditions
                matchesConditions = true;
            else
                switch (rule.Type)
                {
                    case TypeEnum.All:
                        matchesConditions = rule.Conditions.All(condition => ContextMatchesCondition(context, condition, segmentKey, flagDependencies));
                        break;
                    case TypeEnum.Any:
                        matchesConditions = rule.Conditions.Any(condition => ContextMatchesCondition(context, condition, segmentKey, flagDependencies));
                        break;
                    case TypeEnum.None:
                        matchesConditions = !rule.Conditions.Any(condition => ContextMatchesCondition(context, condition, segmentKey, flagDependencies));
                        break;
                    default:
                        matchesConditions = false;
                        break;
                }

            if (!matchesConditions)
                return false;

            if (rule.Rules is null || !rule.Rules.Any())
                return true;

            bool matchesSubRules;
            switch (rule.Type)
            {
                case TypeEnum.All:
                    matchesSubRules = rule.Rules.All(r => ContextMatchesRule(context, r, segmentKey, flagDependencies));
                    break;
                case TypeEnum.Any:
                    matchesSubRules = rule.Rules.Any(r => ContextMatchesRule(context, r, segmentKey, flagDependencies));
                    break;
                case TypeEnum.None:
                    matchesSubRules = !rule.Rules.Any(r => ContextMatchesRule(context, r, segmentKey, flagDependencies));
                    break;
                default:
                    matchesSubRules = false;
                    break;
            }
            return matchesSubRules;
        }

        private static bool ContextMatchesCondition<_, __>(EvaluationContext<_, __> context, Condition condition, string segmentKey, FlagDependencies<_, __> flagDependencies)
        {
            var contextValue = GetContextValue(context, condition.Property, flagDependencies);

            switch (condition.Operator)
            {
                case Operator.In:
                    if (contextValue == null || contextValue.GetType() == typeof(bool))
                        return false;
                    HashSet<string> inValues;
                    if (condition.Value.StringArray != null)
                    {
                        inValues = new HashSet<string>(condition.Value.StringArray);
                    }
                    else
                    {
                        try
                        {
                            inValues = new HashSet<string>(JsonConvert.DeserializeObject<string[]>(condition.Value.String));
                        }
                        catch (JsonException)
                        {
                            inValues = new HashSet<string>(condition.Value.String.Split(','));
                        }
                    }
                    return inValues.Contains(contextValue.ToString());

                case Operator.PercentageSplit:
                    List<string> objectIds;

                    if (contextValue != null)
                        objectIds = new List<string> { segmentKey, contextValue.ToString() };
                    else if (string.IsNullOrEmpty(condition.Property) && context.Identity?.Key != null)
                        objectIds = new List<string> { segmentKey, context.Identity.Key };
                    else
                        return false;

                    float floatConditionValue;

                    try
                    {
                        floatConditionValue = float.Parse(condition.Value.String);
                    }
                    catch (FormatException)
                    {
                        return false;
                    }

                    return Hashing.GetHashedPercentageForObjectIds(objectIds) <= floatConditionValue;

                case Operator.IsNotSet:
                    return contextValue == null;

                case Operator.IsSet:
                    return contextValue != null;

                default:
                    if (contextValue == null)
                        return false;

                    return MatchesContextValue(contextValue, condition);
            }
        }

        private static object GetContextValue<_, __>(EvaluationContext<_, __> context, string property, FlagDependencies<_, __> flagDependencies)
        {
            object value = null;
            if (!(context.Identity?.Traits?.TryGetValue(property, out value) ?? false))
            {
                if (property.StartsWith("$."))
                {
                    var root = TryGetFlagDependencyName(property, out var featureName)
                        ? GetFlagsRoot(featureName, flagDependencies.Get(featureName))
                        : JToken.FromObject(context);
                    var jToken = root.SelectToken(property);
                    if (jToken is JValue jValue)
                    {
                        value = jValue.ToObject<object>();
                    }
                }
            }
            return value;
        }

        private static FlagResult<FeatureMetadataT> GetFlagResult<_, FeatureMetadataT>(EvaluationContext<_, FeatureMetadataT> context, FeatureContext<FeatureMetadataT> featureContext, String reason)
        {
            FlagResult<FeatureMetadataT> flagResult = null;
            string variantKey = null;
            var key = context?.Identity?.Key;

            if (key != null && featureContext.Variants != null && featureContext.Variants.Any())
            {
                variantKey = ControlVariant;
                var percentageValue = Hashing.GetHashedPercentageForObjectIds(new List<string>() { featureContext.Key, key });
                var startPercentage = 0.0f;
                float limit;

                foreach (var variant in featureContext.Variants.OrderBy(v => v.Priority))
                {
                    var weight = (float)variant.Weight;
                    limit = weight + startPercentage;
                    if (startPercentage <= percentageValue && percentageValue < limit)
                    {
                        flagResult = new FlagResult<FeatureMetadataT>
                        {
                            Name = featureContext.Name,
                            Enabled = featureContext.Enabled,
                            Value = variant.Value,
                            Metadata = featureContext.Metadata,
                            Reason = FormattableString.Invariant($"SPLIT; weight={weight}"),
                            Variant = variant.Key,
                        };
                        break;
                    }
                    startPercentage += weight;
                }
            }

            if (flagResult is null)
            {
                flagResult = new FlagResult<FeatureMetadataT>
                {
                    Name = featureContext.Name,
                    Enabled = featureContext.Enabled,
                    Value = featureContext.Value,
                    Metadata = featureContext.Metadata,
                    Reason = reason,
                    Variant = variantKey,
                };
            }

            return flagResult;
        }

        private static bool MatchesContextValue(object contextValue, Condition condition)
        {
            switch (condition.Operator)
            {
                case Operator.NotContains:
                    return !contextValue.ToString().Contains(condition.Value.String);
                case Operator.Regex:
                    return Regex.Match(contextValue.ToString(), condition.Value.String).Success;
                case Operator.Modulo:
                    return EvaluateModulo(contextValue.ToString(), condition.Value.String);
                default:
                    return MatchingFunctionName(contextValue, condition);
            }
        }

        private static bool EvaluateModulo(string contextValue, string conditionValue)
        {
            try
            {
                string[] parts = conditionValue.Split('|');
                if (parts.Length != 2) { return false; }

                double divisor = Convert.ToDouble(parts[0]);
                double remainder = Convert.ToDouble(parts[1]);

                return Convert.ToDouble(contextValue) % divisor == remainder;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool MatchingFunctionName(object contextValue, Condition condition)
        {
            switch (contextValue.GetType().FullName)
            {
                case "System.Int32":
                    return IntOperations((Int32)contextValue, condition);
                case "System.Int64":
                    return LongOperations((Int64)contextValue, condition);
                case "System.Double":
                    return DoubleOperations((double)contextValue, condition);
                case "System.Boolean":
                    return BoolOperations((bool)contextValue, condition);
                default:
                    return StringOperations((string)contextValue, condition);
            }
        }

        private static bool StringOperations(string contextValue, Condition condition)
        {
            var conditionValue = condition.Value.String;

            if (conditionValue.EndsWith(":semver"))
            {
                return SemVerOperations(contextValue, condition);
            }

            switch (condition.Operator)
            {
                case Operator.Equal: return contextValue == conditionValue;
                case Operator.NotEqual: return contextValue != conditionValue;
                case Operator.Contains: return contextValue.Contains(conditionValue);
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool LongOperations(long contextValue, Condition condition)
        {
            long conditionValue;
            try
            {
                conditionValue = InvariantConvert.ToInt64(condition.Value.String);
            }
            catch (FormatException)
            {
                return false;
            }
            switch (condition.Operator)
            {
                case Operator.Equal: return contextValue == conditionValue;
                case Operator.NotEqual: return contextValue != conditionValue;
                case Operator.GreaterThan: return contextValue > conditionValue;
                case Operator.GreaterThanInclusive: return contextValue >= conditionValue;
                case Operator.LessThan: return contextValue < conditionValue;
                case Operator.LessThanInclusive: return contextValue <= conditionValue;
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool IntOperations(long contextValue, Condition condition)
        {
            switch (condition.Operator)
            {
                case Operator.Equal: return contextValue == InvariantConvert.ToInt32(condition.Value.String);
                case Operator.NotEqual: return contextValue != InvariantConvert.ToInt32(condition.Value.String);
                case Operator.GreaterThan: return contextValue > InvariantConvert.ToInt32(condition.Value.String);
                case Operator.GreaterThanInclusive: return contextValue >= InvariantConvert.ToInt32(condition.Value.String);
                case Operator.LessThan: return contextValue < InvariantConvert.ToInt32(condition.Value.String);
                case Operator.LessThanInclusive: return contextValue <= InvariantConvert.ToInt32(condition.Value.String);
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool DoubleOperations(double contextValue, Condition condition)
        {
            switch (condition.Operator)
            {
                case Operator.Equal: return contextValue == InvariantConvert.ToDouble(condition.Value.String);
                case Operator.NotEqual: return contextValue != InvariantConvert.ToDouble(condition.Value.String);
                case Operator.GreaterThan: return contextValue > InvariantConvert.ToDouble(condition.Value.String);
                case Operator.GreaterThanInclusive: return contextValue >= InvariantConvert.ToDouble(condition.Value.String);
                case Operator.LessThan: return contextValue < InvariantConvert.ToDouble(condition.Value.String);
                case Operator.LessThanInclusive: return contextValue <= InvariantConvert.ToDouble(condition.Value.String);
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool BoolOperations(bool contextValue, Condition condition)
        {
            switch (condition.Operator)
            {
                case Operator.Equal: return contextValue == ToBoolean(condition.Value.String);
                case Operator.NotEqual: return contextValue != ToBoolean(condition.Value.String);
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool SemVerOperations(string contextValue, Condition condition)
        {
            try
            {
                string conditionValue = condition.Value.String.Substring(0, condition.Value.String.Length - 7);
                SemVersion conditionValueAsVersion = SemVersion.Parse(conditionValue, SemVersionStyles.Strict);
                SemVersion contextValueAsVersion = SemVersion.Parse(contextValue, SemVersionStyles.Strict);

                switch (condition.Operator)
                {
                    case Operator.Equal: return contextValueAsVersion == conditionValueAsVersion;
                    case Operator.NotEqual: return contextValueAsVersion != conditionValueAsVersion;
                    case Operator.GreaterThan: return contextValueAsVersion.ComparePrecedenceTo(conditionValueAsVersion) > 0;
                    case Operator.GreaterThanInclusive: return contextValueAsVersion.ComparePrecedenceTo(conditionValueAsVersion) >= 0;
                    case Operator.LessThan: return contextValueAsVersion.ComparePrecedenceTo(conditionValueAsVersion) < 0;
                    case Operator.LessThanInclusive: return contextValueAsVersion.ComparePrecedenceTo(conditionValueAsVersion) <= 0;
                    default: throw new ArgumentException("Invalid Operator");
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool ToBoolean(string conditionValue) => !new[] { "false", "False" }.Contains(conditionValue);
    }
}
