using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Identity.Models;

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
            };
        [Theory]
        [MemberData(nameof(TestCasesIdentityInSegment))]
        public void TestIdentityInSegment(SegmentModel segment, List<TraitModel> identityTraits, bool expectedResult)
        {
            var identity = new IdentityModel()
            {
                Identifier = "foo",
                IdentityTraits = identityTraits,
                EnvironmentApiKey = "api-key",
            };
            Assert.Equal(expectedResult, Evaluator.EvaluateIdentityInSegment(identity, segment, null));
        }
        public static IEnumerable<object[]> TestCasesIdentityInSegment() =>
            new List<object[]> {
                new object[] { fixtures.emptySegment, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentSingleCondition, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentSingleCondition, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 } }, true },
                new object[] { fixtures.SegmentMultipleConditionsAll, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentMultipleConditionsAll, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 } }, false },
                new object[] { fixtures.SegmentMultipleConditionsAll, new List<TraitModel> {
                                    new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = fixtures.TraitKey2, TraitValue = fixtures.TraitValue2 }
                                }, true
                },
                new object[] { fixtures.SegmentMultipleConditionsAny, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentMultipleConditionsAny, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 } }, true },
                new object[] { fixtures.SegmentMultipleConditionsAny, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey2, TraitValue = fixtures.TraitValue2 } }, true },
                new object[] { fixtures.SegmentMultipleConditionsAny, new List<TraitModel> {
                                    new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = fixtures.TraitKey2, TraitValue = fixtures.TraitValue2 }
                                    }, true
                },
                new object[] { fixtures.SegmentNestedRules, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentNestedRules, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 } }, false },
                new object[] { fixtures.SegmentNestedRules, new List<TraitModel> {
                                    new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = fixtures.TraitKey2, TraitValue = fixtures.TraitValue2 },
                                    new TraitModel { TraitKey = fixtures.TraitKey3, TraitValue = fixtures.TraitValue3 }
                                    }, true
                },
                new object[] { fixtures.SegmentConditionsAndNestedRules, new List<TraitModel>(), false },
                new object[] { fixtures.SegmentConditionsAndNestedRules, new List<TraitModel> { new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 } }, false },
                new object[] { fixtures.SegmentConditionsAndNestedRules, new List<TraitModel> {
                                    new TraitModel { TraitKey = fixtures.TraitKey1, TraitValue = fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = fixtures.TraitKey2, TraitValue = fixtures.TraitValue2 },
                                    new TraitModel { TraitKey = fixtures.TraitKey3, TraitValue = fixtures.TraitValue3 }
                                    }, true
                },
            };

    }
}
