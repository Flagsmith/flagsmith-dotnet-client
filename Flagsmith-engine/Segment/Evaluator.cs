using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlagsmithEngine.Segment
{
    public static class Evaluator
    {

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
                return new Hashing().GetHashedPercentageForObjectIds(new List<string>() { segemntId, identityId }) <= float.Parse(condition.Value);

            var trait = identityTraits.FirstOrDefault(t => t.TraitKey == condition.Property);
            if (trait != null)
                return MatchesTraitValue(trait.TraitValue, condition);
            return false;
        }
        public static bool MatchesTraitValue(object traitValue, SegmentConditionModel condition)
        {
            var exceptionOperatorMethods = new Dictionary<string, string>(){
                {Constants.NotContains, "EvaluateNotContains"},
                {Constants.Regex, "EvaluateRegex"},
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

    }
}
