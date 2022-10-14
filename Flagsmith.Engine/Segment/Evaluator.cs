using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Utils;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            return segment.Rules?.Any() == true && segment.Rules.All(rule => TraitsMatchSegmentRule(traits, rule, segment.Id.ToString(), identity.CompositeKey));
        }
        static bool TraitsMatchSegmentRule(List<TraitModel> identityTraits, SegmentRuleModel rule, string segmentId, string identityId)
        {
            var matchesConditions = rule.Conditions.Any() ?
              rule.MatchingFunction(rule.Conditions.Select(c =>
              TraitsMatchSegmentCondition(identityTraits, c, segmentId, identityId)).ToList()
              ) : true;
            return matchesConditions && (rule.Rules?.All(r => TraitsMatchSegmentRule(identityTraits, r, segmentId, identityId)) ?? true);
        }
        static bool TraitsMatchSegmentCondition(List<TraitModel> identityTraits, SegmentConditionModel condition, string segemntId, string identityId)
        {
            if (condition.Operator == Constants.PercentageSplit)
                return Hashing.GetHashedPercentageForObjectIds(new List<string>() { segemntId, identityId }) <= float.Parse(condition.Value);

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
            var exceptionOperatorMethods = new Dictionary<string, string>(){
                {Constants.NotContains, "EvaluateNotContains"},
                {Constants.Regex, "EvaluateRegex"},
                {Constants.Modulo, "EvaluateModulo"},
            };
            if (exceptionOperatorMethods.ContainsKey(condition.Operator))
                return (bool)typeof(SegmentConditionModel).GetMethod(exceptionOperatorMethods[condition.Operator]).Invoke(condition, new object[] { traitValue });
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
                default: throw new ArgumentException("Invalid Operator");
            }
        }
        static bool longOperations(long traitValue, SegmentConditionModel condition)
        {
            var currentValue = Convert.ToInt64(condition.Value);
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == currentValue;
                case Constants.NotEqual: return traitValue != currentValue;
                case Constants.GreaterThan: return traitValue > currentValue;
                case Constants.GreaterThanInclusive: return traitValue >= currentValue;
                case Constants.LessThan: return traitValue < currentValue;
                case Constants.LessThanInclusive: return traitValue <= currentValue;
                default: throw new ArgumentException("Invalid Operator");
            }
        }
        static bool intOperations(long traitValue, SegmentConditionModel condition)
        {
            var currentValue = Convert.ToInt32(condition.Value);
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == currentValue;
                case Constants.NotEqual: return traitValue != currentValue;
                case Constants.GreaterThan: return traitValue > currentValue;
                case Constants.GreaterThanInclusive: return traitValue >= currentValue;
                case Constants.LessThan: return traitValue < currentValue;
                case Constants.LessThanInclusive: return traitValue <= currentValue;
                default: throw new ArgumentException("Invalid Operator");
            }
        }
        static bool doubleOperations(double traitValue, SegmentConditionModel condition)
        {
            var currentValue = Convert.ToDouble(condition.Value);
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == currentValue;
                case Constants.NotEqual: return traitValue != currentValue;
                case Constants.GreaterThan: return traitValue > currentValue;
                case Constants.GreaterThanInclusive: return traitValue >= currentValue;
                case Constants.LessThan: return traitValue < currentValue;
                case Constants.LessThanInclusive: return traitValue <= currentValue;
                default: throw new ArgumentException("Invalid Operator");
            }
        }
        static bool boolOperations(bool traitValue, SegmentConditionModel condition)
        {
            var currentValue = Convert.ToBoolean(condition.Value);
            switch (condition.Operator)
            {
                case Constants.Equal: return traitValue == currentValue;
                case Constants.NotEqual: return traitValue != currentValue;
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
