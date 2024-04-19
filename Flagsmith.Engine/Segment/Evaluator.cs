﻿using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Utils;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlagsmithEngine.Segment
{
    public static class Evaluator
    {
        public static Hashing Hashing = new Hashing();

        public static List<SegmentModel> GetIdentitySegments(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits)
            => environmentModel.Project.Segments.Where(s => EvaluateIdentityInSegment(identity, s, overrideTraits)).ToList();

        public static bool EvaluateIdentityInSegment(IdentityModel identity, SegmentModel segment, List<TraitModel> overrideTraits)
        {
            var traits = overrideTraits?.Any() == true ? overrideTraits : identity.IdentityTraits;
            var identityHashKey = identity.DjangoId == null ? identity.CompositeKey : identity.DjangoId.ToString();
            return segment.Rules?.Any() == true && segment.Rules.All(rule => TraitsMatchSegmentRule(traits, rule, segment.Id.ToString(), identityHashKey));
        }

        static bool TraitsMatchSegmentRule(List<TraitModel> identityTraits, SegmentRuleModel rule, string segmentId, string identityId)
        {
            var matchesConditions = !rule.Conditions.Any() || rule.MatchingFunction(rule.Conditions.Select(c =>
                    TraitsMatchSegmentCondition(identityTraits, c, segmentId, identityId)).ToList()
            );
            return matchesConditions && (rule.Rules?.All(r => TraitsMatchSegmentRule(identityTraits, r, segmentId, identityId)) ?? true);
        }

        static bool TraitsMatchSegmentCondition(List<TraitModel> identityTraits, SegmentConditionModel condition, string segmentId, string identityId)
        {
            if (condition.Operator == Constants.PercentageSplit)
                return Hashing.GetHashedPercentageForObjectIds(new List<string>() { segmentId, identityId }) <= float.Parse(condition.Value);

            var trait = identityTraits?.FirstOrDefault(t => t.TraitKey == condition.Property);

            if (condition.Operator == Constants.IsSet)
            {
                return trait != null;
            }
            else if (condition.Operator == Constants.IsNotSet)
            {
                return trait == null;
            }

            return trait != null && MatchesTraitValue(trait.TraitValue, condition);
        }

        public static bool MatchesTraitValue(object traitValue, SegmentConditionModel condition)
        {
            var exceptionOperatorMethods = new Dictionary<string, string>()
            {
                { Constants.NotContains, "EvaluateNotContains" },
                { Constants.Regex, "EvaluateRegex" },
                { Constants.Modulo, "EvaluateModulo" },
            };

            if (exceptionOperatorMethods.TryGetValue(condition.Operator, out var operatorMethod))
            {
                return (bool)typeof(SegmentConditionModel).GetMethod(operatorMethod).Invoke(condition, new object[] { traitValue.ToString() });
            }

            return MatchingFunctionName(traitValue, condition);
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
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == Convert.ToInt64(condition.Value);
                case Constants.NotEqual: return traitValue != Convert.ToInt64(condition.Value);
                case Constants.GreaterThan: return traitValue > Convert.ToInt64(condition.Value);
                case Constants.GreaterThanInclusive: return traitValue >= Convert.ToInt64(condition.Value);
                case Constants.LessThan: return traitValue < Convert.ToInt64(condition.Value);
                case Constants.LessThanInclusive: return traitValue <= Convert.ToInt64(condition.Value);
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
                case Constants.Equal: return traitValue == Convert.ToBoolean(condition.Value);
                case Constants.NotEqual: return traitValue != Convert.ToBoolean(condition.Value);
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
    }
}