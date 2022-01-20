using Flagsmith_engine.Environment.Models;
using Flagsmith_engine.Identity.Models;
using Flagsmith_engine.Segment.Models;
using Flagsmith_engine.Trait.Models;
using Flagsmith_engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flagsmith_engine.Segment
{
    public static class Evaluator
    {

        public static List<SegmentModel> GetIdentitySegments(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits)
            => environmentModel.Project.Segments.Where(s => EvaluateIdentityInSegment(identity, s, overrideTraits)).ToList();

        static bool EvaluateIdentityInSegment(IdentityModel identity, SegmentModel segment, List<TraitModel> overrideTraits)
        {
            var traits = overrideTraits?.Any() == true ? overrideTraits : identity.IdentityTraits;
            return segment.Rules.Any() && segment.Rules.All(rule => TraitsMatchSegmentRule(traits, rule, segment.Id.ToString(), identity.CompositeKey));
        }
        static bool TraitsMatchSegmentRule(List<TraitModel> identityTraits, SegmentRuleModel rule, string segemntId, string identityId)
        {
            var matchesConditions = rule.Conditions.Any() ?
              rule.MatchingFunction(rule.Conditions.Select(c =>
              TraitsMatchSegmentCondition(identityTraits, c, segemntId, identityId)).ToList()
              ) : true;
            return matchesConditions && rule.Rules.All(r => TraitsMatchSegmentRule(identityTraits, r, segemntId, identityId));
        }
        static bool TraitsMatchSegmentCondition(List<TraitModel> identityTraits, SegmentConditionModel condition, string segemntId, string identityId)
        {
            if (condition.Operator == Constants.PercentageSplit)
                return Hashing.GetHashedPercentageForObjectIds(new List<string>() { segemntId, identityId }) <= float.Parse(condition.Value);

            var trait = identityTraits.FirstOrDefault(t => t.TraitKey == condition.Property);
            if (trait != null)
                return MatchesTraitValue(trait.TraitValue, condition);
            return false;
        }
        static bool MatchesTraitValue(object traitValue, SegmentConditionModel condition)
        {
            var exceptionOperatorMethods = new Dictionary<string, string>(){
                {Constants.NotContains, "EvaluateNotContains"},
                {Constants.Regex, "EvaluateRegex"},
            };
            if (exceptionOperatorMethods.ContainsKey(condition.Operator))
                return (bool)typeof(SegmentConditionModel).GetMethod(condition.Operator).Invoke(condition, new object[] { traitValue });
            return MatchingFunctionName(traitValue, condition);
        }
        static bool MatchingFunctionName(object traitValue, SegmentConditionModel condition)
        {
            switch (getObjectType(condition.Value))
            {
                case "System.Int32":
                    return intOperations((Int64)traitValue, condition);
                case "System.Double":
                    return doubleOperations((double)traitValue, condition);
                case "System.Boolean":
                    return boolOperations((bool)traitValue, condition);
                default:
                    return stringOperations((string)traitValue, condition);
            }
        }
        static string getObjectType(string val)
        {
            int _intVal;
            bool _booleanVal;
            double _doubleVal;


            if (int.TryParse(val, out _intVal) && _intVal.ToString() == val)
                return typeof(int).FullName;
            else if (double.TryParse(val, out _doubleVal) && _doubleVal.ToString() == val)
                return typeof(double).FullName;
            else if (bool.TryParse(val, out _booleanVal) && _booleanVal.ToString() == val)
                return typeof(bool).FullName;
            else
                return typeof(string).FullName;
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
