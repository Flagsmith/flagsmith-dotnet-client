using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Json.Path;
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

        private static readonly ConcurrentDictionary<string, JsonPath> JsonPaths = new ConcurrentDictionary<string, JsonPath>();

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
                flagDependencies.ApplyTo(result.Flags);
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

            foreach (var segmentItem in context.Segments)
            {
                var segmentContext = segmentItem.Value;

                var cycleHits = flagDependencies.CycleHits;
                if (!IsContextInSegment(context, segmentContext, flagDependencies) || flagDependencies.CycleHits != cycleHits)
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
        /// Whether a segment override takes precedence over the one held so far: the lowest priority wins, and the first seen wins a tie.
        /// </summary>
        private static bool WinsOver<FeatureMetadataT>(FeatureContext<FeatureMetadataT> candidate, FeatureContext<FeatureMetadataT> incumbent)
        {
            return incumbent is null || (candidate.Priority ?? WeakestPriority) < (incumbent.Priority ?? WeakestPriority);
        }

        private static Dictionary<string, FlagResult<FeatureMetadataT>> EvaluateFlags<Any, FeatureMetadataT>(EvaluationContext<Any, FeatureMetadataT> context, Dictionary<string, SegmentOverride<FeatureMetadataT>> segmentOverrides)
        {
            var flags = new Dictionary<string, FlagResult<FeatureMetadataT>>();

            if (context?.Features is null)
                return flags;

            var featureNamesWithSegmentOverrides = new HashSet<string>(
                (context.Segments?.Values ?? Enumerable.Empty<SegmentContext<Any, FeatureMetadataT>>())
                    .SelectMany(segment => segment.Overrides ?? Enumerable.Empty<FeatureContext<FeatureMetadataT>>())
                    .Select(segmentOverride => segmentOverride.Name)
            );

            foreach (var featureContext in context.Features.Values)
            {
                segmentOverrides.TryGetValue(featureContext.Name, out var segmentOverride);
                flags[featureContext.Name] = GetFeatureFlagResult(
                    context,
                    featureContext,
                    segmentOverride,
                    featureNamesWithSegmentOverrides.Contains(featureContext.Name)
                );
            }

            return flags;
        }

        /// <summary>
        /// Get the flag result of a feature, served by its segment override if any, or else by its environment default:
        /// DEFAULT if the feature has segment overrides that did not match, STATIC if it has none.
        /// </summary>
        private static FlagResult<FeatureMetadataT> GetFeatureFlagResult<_, FeatureMetadataT>(EvaluationContext<_, FeatureMetadataT> context, FeatureContext<FeatureMetadataT> featureContext, SegmentOverride<FeatureMetadataT> segmentOverride, bool hasSegmentOverrides)
        {
            if (segmentOverride != null)
                return GetFlagResult(context, segmentOverride.FeatureContext, $"TARGETING_MATCH; segment={segmentOverride.SegmentName}");
            return GetFlagResult(context, featureContext, hasSegmentOverrides ? "DEFAULT" : "STATIC");
        }

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
                var jsonPath = property.StartsWith("$.") ? JsonPaths.GetOrAdd(property, ParseJsonPath) : null;
                if (jsonPath != null)
                {
                    value = EvaluateJsonPath(context, property, jsonPath, flagDependencies);
                }
            }
            return value;
        }

        // A trait starting with "$." but not a valid JSONPath is read as a trait key only.
        private static JsonPath ParseJsonPath(string property) => JsonPath.TryParse(property, out var jsonPath) ? jsonPath : null;

        private static object EvaluateJsonPath<_, __>(EvaluationContext<_, __> context, string property, JsonPath jsonPath, FlagDependencies<_, __> flagDependencies)
        {
            try
            {
                var matches = jsonPath.Evaluate(GetJsonPathRoot(context, jsonPath, flagDependencies)).Matches;
                return matches?.Count > 0 && matches[0].Value is JsonValue jsonValue ? ToContextValue(jsonValue) : null;
            }
            catch (Exception exception) // Not expected to happen: the query is valid and the context serialisable
            {
                Trace.TraceWarning($"Failed to evaluate JSONPath query '{property}' in context: {exception.Message}");
                return null;
            }
        }

        // Serialise only the context member a JSONPath reads: serialising every feature and segment dominates evaluation.
        private static JsonNode GetJsonPathRoot<_, __>(EvaluationContext<_, __> context, JsonPath jsonPath, FlagDependencies<_, __> flagDependencies)
        {
            switch (GetSegmentName(jsonPath, 0))
            {
                case "environment":
                    return new JsonObject { ["environment"] = ToJsonNode(context.Environment) };
                case "identity":
                    return new JsonObject { ["identity"] = ToJsonNode(context.Identity) };
                case "flags":
                    var featureName = GetSegmentName(jsonPath, 1);
                    var flag = featureName is null ? null : flagDependencies.Get(featureName);
                    return new JsonObject { ["flags"] = flag is null ? new JsonObject() : new JsonObject { [featureName] = ToJsonNode(flag) } };
                default:
                    return ToJsonNode(context);
            }
        }

        private static string GetSegmentName(JsonPath jsonPath, int index)
        {
            if (jsonPath.Segments.Length <= index || jsonPath.Segments[index].IsRecursive || jsonPath.Segments[index].Selectors.Length != 1)
                return null;
            return (jsonPath.Segments[index].Selectors[0] as NameSelector)?.Name;
        }

        private static JsonNode ToJsonNode(EnvironmentContext environment) => new JsonObject { ["key"] = environment.Key, ["name"] = environment.Name };

        private static JsonNode ToJsonNode(IdentityContext identity)
        {
            if (identity is null)
                return null;
            var jsonObject = new JsonObject { ["identifier"] = identity.Identifier };
            if (identity.Key != null)
                jsonObject["key"] = identity.Key;
            if (identity.Traits != null)
            {
                var traits = new JsonObject();
                foreach (var trait in identity.Traits)
                    traits[trait.Key] = ToJsonNode(trait.Value);
                jsonObject["traits"] = traits;
            }
            return jsonObject;
        }

        // Converts in memory: a round trip through a JSON string dominates evaluation.
        private static JsonNode ToJsonNode(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    return JsonValue.Create(text);
                case bool boolean:
                    return JsonValue.Create(boolean);
                case int integer:
                    return JsonValue.Create((long)integer);
                case long integer:
                    return JsonValue.Create(integer);
                case double number:
                    return JsonValue.Create(number);
                case JToken token:
                    return ToJsonNode(token);
                default:
                    return ToJsonNode(JToken.FromObject(value));
            }
        }

        private static JsonNode ToJsonNode(JToken token)
        {
            switch (token)
            {
                case JObject jObject:
                    var jsonObject = new JsonObject();
                    foreach (var property in jObject)
                        jsonObject[property.Key] = ToJsonNode(property.Value);
                    return jsonObject;
                case JArray jArray:
                    var jsonArray = new JsonArray();
                    foreach (var item in jArray)
                        jsonArray.Add(ToJsonNode(item));
                    return jsonArray;
                case JValue jValue:
                    switch (jValue.Value)
                    {
                        case null:
                            return null;
                        case string _:
                        case bool _:
                        case long _:
                        case double _:
                            return ToJsonNode(jValue.Value);
                        default:
                            return JsonValue.Create(jValue.ToString(CultureInfo.InvariantCulture));
                    }
                default:
                    return null;
            }
        }

        private static object ToContextValue(JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var text))
                return text;
            if (jsonValue.TryGetValue<bool>(out var boolean))
                return boolean;
            if (jsonValue.TryGetValue<long>(out var integer))
                return integer;
            if (jsonValue.TryGetValue<double>(out var number))
                return number;
            return null;
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
                    return EvaluateRegex(contextValue.ToString(), condition.Value.String);
                case Operator.Modulo:
                    return EvaluateModulo(contextValue.ToString(), condition.Value.String);
                default:
                    return MatchingFunctionName(contextValue, condition);
            }
        }

        // Regex conditions match from the start of the value, but do not need to match the whole value.
        private static bool EvaluateRegex(string contextValue, string conditionValue)
        {
            var match = Regex.Match(contextValue, conditionValue);
            return match.Success && match.Index == 0;
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
                    return LongOperations((Int32)contextValue, condition);
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
                case Operator.Contains: return false;
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        private static bool DoubleOperations(double contextValue, Condition condition)
        {
            double conditionValue;
            try
            {
                conditionValue = InvariantConvert.ToDouble(condition.Value.String);
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
                case Operator.Contains: return false;
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
