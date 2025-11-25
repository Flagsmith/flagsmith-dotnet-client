using System.Collections.Generic;
using Xunit;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;

namespace EngineTest.Unit.Segments
{
    public class SegmentEvaluatorTest
    {
        [Theory]
        [MemberData(nameof(TestCasesSegmentCondition))]
        public void TestSegmentConditionMatchesTraitValue(string _operator, object traitValue, string conditionValue, bool expectedResult)
        {
            SegmentConditionModel conditionModel = new SegmentConditionModel { Operator = _operator, Value = conditionValue, Property = "foo" };
            Assert.Equal(expectedResult, Evaluator.MatchesTraitValue(traitValue, conditionModel));
        }
        public static IEnumerable<object[]> TestCasesSegmentCondition() =>
            new List<object[]>
            {
                new object[]{ Constants.Equal, "bar", "bar", true },
                new object[]{Constants.Equal, "bar", "baz", false},
                new object[]{Constants.Equal, 1, "1", true},
                new object[]{Constants.Equal, 1, "2", false},
                new object[]{Constants.Equal, true, "true", true},
                new object[]{Constants.Equal, false, "false", true},
                new object[]{Constants.Equal, false, "true", false},
                new object[]{Constants.Equal, true, "false", false},
                new object[]{Constants.Equal, 1.23, "1.23", true},
                new object[]{Constants.Equal, 1.23, "4.56", false},
                new object[]{Constants.GreaterThan, 2, "1", true},
                new object[]{Constants.GreaterThan, 1, "1", false},
                new object[]{Constants.GreaterThan, 0, "1", false},
                new object[]{Constants.GreaterThan, 2.1, "2.0", true},
                new object[]{Constants.GreaterThan, 2.1, "2.1", false},
                new object[]{Constants.GreaterThan, 2.0, "2.1", false},
                new object[]{Constants.GreaterThanInclusive, 2, "1", true},
                new object[]{Constants.GreaterThanInclusive, 1, "1", true},
                new object[]{Constants.GreaterThanInclusive, 0, "1", false},
                new object[]{Constants.GreaterThanInclusive, 2.1, "2.0", true},
                new object[]{Constants.GreaterThanInclusive, 2.1, "2.1", true},
                new object[]{Constants.GreaterThanInclusive, 2.0, "2.1", false},
                new object[]{Constants.LessThan, 1, "2", true},
                new object[]{Constants.LessThan, 1, "1", false},
                new object[]{Constants.LessThan, 1, "0", false},
                new object[]{Constants.LessThan, 2.0, "2.1", true},
                new object[]{Constants.LessThan, 2.1, "2.1", false},
                new object[]{Constants.LessThan, 2.1, "2.0", false},
                new object[]{Constants.LessThanInclusive, 1, "2", true},
                new object[]{Constants.LessThanInclusive, 1, "1", true},
                new object[]{Constants.LessThanInclusive, 1, "0", false},
                new object[]{Constants.LessThanInclusive, 2.0, "2.1", true},
                new object[]{Constants.LessThanInclusive, 2.1, "2.1", true},
                new object[]{Constants.LessThanInclusive, 2.1, "2.0", false},
                new object[]{Constants.NotEqual, "bar", "baz", true},
                new object[]{Constants.NotEqual, "bar", "bar", false},
                new object[]{Constants.NotEqual, 1, "2", true},
                new object[]{Constants.NotEqual, 1, "1", false},
                new object[]{Constants.NotEqual, true, "false", true},
                new object[]{Constants.NotEqual, false, "true", true},
                new object[]{Constants.NotEqual, false, "false", false},
                new object[]{Constants.NotEqual, true, "true", false},
                new object[]{Constants.Contains, "bar", "b", true},
                new object[]{Constants.Contains, "bar", "bar", true},
                new object[]{Constants.Contains, "bar", "baz", false},
                new object[]{Constants.NotContains, "bar", "b", false},
                new object[]{Constants.NotContains, "bar", "bar", false},
                new object[]{Constants.NotContains, "bar", "baz", true},
                new object[]{Constants.Regex, "foo", @"[a-z]+", true},
                new object[]{Constants.Regex, "FOO", @"[a-z]+", false},
                new object[]{Constants.Regex, "1.2.3", @"\d", true},
                new object[]{Constants.Equal, "1.0.0", "1.0.0:semver", true},
                new object[]{Constants.Equal, "1.0.0", "1.0.1:semver", false},
                new object[]{Constants.NotEqual, "1.0.0", "1.0.0:semver", false},
                new object[]{Constants.NotEqual, "1.0.0", "1.0.1:semver", true},
                new object[]{Constants.GreaterThan, "1.0.1", "1.0.0:semver", true},
                new object[]{Constants.GreaterThan, "1.0.0", "1.0.0-beta:semver", true},
                new object[]{Constants.GreaterThan, "1.0.1", "1.2.0:semver", false},
                new object[]{Constants.GreaterThan, "1.0.1", "1.0.1:semver", false},
                new object[]{Constants.GreaterThan, "1.2.4", "1.2.3-pre.2+build.4:semver", true},
                new object[]{Constants.LessThan, "1.0.0", "1.0.1:semver", true},
                new object[]{Constants.LessThan, "1.0.0", "1.0.0:semver", false},
                new object[]{Constants.LessThan, "1.0.1", "1.0.0:semver", false},
                new object[]{Constants.LessThan, "1.0.0-rc.2", "1.0.0-rc.3:semver", true},
                new object[]{Constants.GreaterThanInclusive, "1.0.1", "1.0.0:semver", true},
                new object[]{Constants.GreaterThanInclusive, "1.0.1", "1.2.0:semver", false},
                new object[]{Constants.GreaterThanInclusive, "1.0.1", "1.0.1:semver", true},
                new object[]{Constants.LessThanInclusive, "1.0.0", "1.0.1:semver", true},
                new object[]{Constants.LessThanInclusive, "1.0.0", "1.0.0:semver", true},
                new object[]{Constants.LessThanInclusive, "1.0.1", "1.0.0:semver", false},
                new object[]{Constants.Modulo, 2, "2|0", true},
                new object[]{Constants.Modulo, 3, "2|0", false},
                new object[]{Constants.Modulo, 2.0, "2|0", true},
                new object[]{Constants.Modulo, 2.0, "2.0|0", true},
                new object[]{Constants.Modulo, "foo", "2|0", false},
                new object[]{Constants.Modulo, "foo", "foo|bar", false},
                new object[]{Constants.In, "foo", "", false},
                new object[]{Constants.In, "foo", "foo,bar", true},
                new object[]{Constants.In, "bar", "foo,bar", true},
                new object[]{Constants.In, "ba", "foo,bar", false},
                new object[]{Constants.In, "foo", "foo", true},
                new object[]{Constants.In, 1, "1,2,3,4", true},
                new object[]{Constants.In, 1, "", false},
                new object[]{Constants.In, 1, "1", true},
                // Flagsmith's engine does not evaluate `IN` condition for floats/doubles and booleans
                // due to ambiguous serialization across supported platforms.
                new object[]{Constants.In, 1.5, "1.5", false},
                new object[]{Constants.In, false, "false", false},
            };
    }
}
