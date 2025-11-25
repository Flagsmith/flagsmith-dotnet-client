using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlagsmithEngine.Segment
{
    public class SegmentOverride<FeatureMetadataT>
    {
        public FeatureContext<FeatureMetadataT> FeatureContext { get; set; }
        public string SegmentName { get; set; }
    }

    public static class ContextEvaluator
    {
        public static Hashing Hashing = new Hashing();

        public static (SegmentResult<SegmentMetadataT>[] Segments, Dictionary<string, SegmentOverride<FeatureMetadataT>> SegmentOverrides) EvaluateSegments<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
        {
            var segmentOverrides = new Dictionary<string, SegmentOverride<FeatureMetadataT>>();

            if (context?.Segments is null)
            { return (Array.Empty<SegmentResult<SegmentMetadataT>>(), segmentOverrides); }

            var segmentResults = new List<SegmentResult<SegmentMetadataT>>();

            foreach (var segmentItem in context.Segments)
            {
                var segmentContext = segmentItem.Value;

                if (!IsContextInSegment(context, segmentContext))
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
                    var featureName = segmentOverride.Name;
                    if (segmentOverrides.ContainsKey(featureName))
                    {
                        var existingPriority = segmentOverrides[featureName].FeatureContext.Priority ?? Constants.WeakestPriority;
                        if ((segmentOverride.Priority ?? Constants.WeakestPriority) > existingPriority)
                            continue;
                    }
                    segmentOverrides[segmentOverride.Name] = new SegmentOverride<FeatureMetadataT>
                    {
                        FeatureContext = segmentOverride,
                        SegmentName = segmentContext.Name
                    };
                }
            }

            return (segmentResults.ToArray(), segmentOverrides);
        }

        public static Dictionary<string, FlagResult<FeatureMetadataT>> EvaluateFlags<Any, FeatureMetadataT>(EvaluationContext<Any, FeatureMetadataT> context, Dictionary<string, SegmentOverride<FeatureMetadataT>> segmentOverrides)
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
                        $"TARGETING_MATCH; segment={segmentOverride.SegmentName}"
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

        private static bool IsContextInSegment<_, __>(EvaluationContext<_, __> context, SegmentContext<_, __> segmentContext)
        {
            return (
                segmentContext?.Rules != null &&
                segmentContext.Rules.All(rule => ContextMatchesRule(
                    context,
                    rule,
                    segmentContext.Key
                ))
            );
        }

        private static bool ContextMatchesRule<_, __>(EvaluationContext<_, __> context, SegmentRule rule, string segmentKey)
        {
            bool matchesConditions;

            if (rule?.Conditions is null || !rule.Conditions.Any())
                // Sometimes rules are just groupers of subrules, having no intrinsic conditions
                matchesConditions = true;
            else
                switch (rule.Type)
                {
                    case TypeEnum.All:
                        matchesConditions = rule.Conditions.All(condition => ContextMatchesCondition(context, condition, segmentKey));
                        break;
                    case TypeEnum.Any:
                        matchesConditions = rule.Conditions.Any(condition => ContextMatchesCondition(context, condition, segmentKey));
                        break;
                    case TypeEnum.None:
                        matchesConditions = !rule.Conditions.Any(condition => ContextMatchesCondition(context, condition, segmentKey));
                        break;
                    default:
                        matchesConditions = false;
                        break;
                }

            return matchesConditions && (rule.Rules?.All(r => ContextMatchesRule(context, r, segmentKey)) ?? true);
        }

        private static bool ContextMatchesCondition<_, __>(EvaluationContext<_, __> context, Condition condition, string segmentKey)
        {
            var contextValue = GetContextValue(context, condition.Property);

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
                    else if (context.Identity?.Key != null)
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

                    var segmentConditionModel = new SegmentConditionModel
                    {
                        Operator = JsonConvert.SerializeObject(condition.Operator).Replace("\"", ""),
                        Value = condition.Value.String,
                        Property = condition.Property
                    };

                    return Evaluator.MatchesContextValue(contextValue, segmentConditionModel);
            }
        }

        private static object GetContextValue<_, __>(EvaluationContext<_, __> context, string property)
        {
            object value = null;
            if (!(context.Identity?.Traits?.TryGetValue(property, out value) ?? false))
            {
                if (property.StartsWith("$."))
                {
                    var jToken = JToken.FromObject(context).SelectToken(property);
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
            var key = context?.Identity?.Key;

            if (key != null && featureContext.Variants != null)
            {
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
                            Reason = $"SPLIT; weight={weight}",
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
                    Reason = reason
                };
            }

            return flagResult;
        }
    }

    public static class Evaluator
    {
        public static bool MatchesContextValue(object contextValue, SegmentConditionModel condition)
        {
            var exceptionOperatorMethods = new Dictionary<string, string>()
            {
                { Constants.NotContains, "EvaluateNotContains" },
                { Constants.Regex, "EvaluateRegex" },
                { Constants.Modulo, "EvaluateModulo" },
            };

            if (exceptionOperatorMethods.TryGetValue(condition.Operator, out var operatorMethod))
            {
                return (bool)typeof(SegmentConditionModel).GetMethod(operatorMethod).Invoke(condition, new object[] { contextValue.ToString() });
            }

            return MatchingFunctionName(contextValue, condition);
        }

        static bool MatchingFunctionName(object traitValue, SegmentConditionModel condition)
        {
            switch (traitValue.GetType().FullName)
            {
                case "System.Int32":
                    return intOperations((Int32)traitValue, condition);
                case "System.Int64":
                    return longOperations((Int64)traitValue, condition);
                case "System.Double":
                    return doubleOperations((double)traitValue, condition);
                case "System.Boolean":
                    return boolOperations((bool)traitValue, condition);
                default:
                    return stringOperations((string)traitValue, condition);
            }
        }

        static bool stringOperations(string traitValue, SegmentConditionModel condition)
        {
            var currentValue = condition.Value;

            if (currentValue.EndsWith(":semver"))
            {
                return semVerOperations(traitValue, condition);
            }

            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == currentValue;
                case Constants.NotEqual: return traitValue != currentValue;
                case Constants.Contains: return traitValue.Contains(currentValue);
                case Constants.In: return condition.Value.Split(',').Contains(traitValue);
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        static bool longOperations(long traitValue, SegmentConditionModel condition)
        {
            long conditionValue;
            try
            {
                conditionValue = Convert.ToInt64(condition.Value);
            }
            catch (FormatException)
            {
                return false;
            }
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == conditionValue;
                case Constants.NotEqual: return traitValue != conditionValue;
                case Constants.GreaterThan: return traitValue > conditionValue;
                case Constants.GreaterThanInclusive: return traitValue >= conditionValue;
                case Constants.LessThan: return traitValue < conditionValue;
                case Constants.LessThanInclusive: return traitValue <= conditionValue;
                case Constants.In: return condition.Value.Split(',').Contains(traitValue.ToString());
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        static bool intOperations(long traitValue, SegmentConditionModel condition)
        {
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == Convert.ToInt32(condition.Value);
                case Constants.NotEqual: return traitValue != Convert.ToInt32(condition.Value);
                case Constants.GreaterThan: return traitValue > Convert.ToInt32(condition.Value);
                case Constants.GreaterThanInclusive: return traitValue >= Convert.ToInt32(condition.Value);
                case Constants.LessThan: return traitValue < Convert.ToInt32(condition.Value);
                case Constants.LessThanInclusive: return traitValue <= Convert.ToInt32(condition.Value);
                case Constants.In: return condition.Value.Split(',').Contains(traitValue.ToString());
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        static bool doubleOperations(double traitValue, SegmentConditionModel condition)
        {
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == Convert.ToDouble(condition.Value);
                case Constants.NotEqual: return traitValue != Convert.ToDouble(condition.Value);
                case Constants.GreaterThan: return traitValue > Convert.ToDouble(condition.Value);
                case Constants.GreaterThanInclusive: return traitValue >= Convert.ToDouble(condition.Value);
                case Constants.LessThan: return traitValue < Convert.ToDouble(condition.Value);
                case Constants.LessThanInclusive: return traitValue <= Convert.ToDouble(condition.Value);
                case Constants.In: return false;
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        static bool boolOperations(bool traitValue, SegmentConditionModel condition)
        {
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == toBoolean(condition.Value);
                case Constants.NotEqual: return traitValue != toBoolean(condition.Value);
                case Constants.In: return false;
                default: throw new ArgumentException("Invalid Operator");
            }
        }

        static bool semVerOperations(string traitValue, SegmentConditionModel condition)
        {
            try
            {
                string conditionValue = condition.Value.Substring(0, condition.Value.Length - 7);
                SemVersion conditionValueAsVersion = SemVersion.Parse(conditionValue, SemVersionStyles.Strict);
                SemVersion traitValueAsVersion = SemVersion.Parse(traitValue, SemVersionStyles.Strict);

                switch (condition.Operator)
                {
                    case Constants.Equal: return traitValueAsVersion == conditionValueAsVersion;
                    case Constants.NotEqual: return traitValueAsVersion != conditionValueAsVersion;
                    case Constants.GreaterThan: return traitValueAsVersion > conditionValueAsVersion;
                    case Constants.GreaterThanInclusive: return traitValueAsVersion >= conditionValueAsVersion;
                    case Constants.LessThan: return traitValueAsVersion < conditionValueAsVersion;
                    case Constants.LessThanInclusive: return traitValueAsVersion <= conditionValueAsVersion;
                    default: throw new ArgumentException("Invalid Operator");
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        static bool toBoolean(string conditionValue) => !new[] { "false", "False" }.Contains(conditionValue);
    }
}
