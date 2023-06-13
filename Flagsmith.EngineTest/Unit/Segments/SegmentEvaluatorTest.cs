using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Utils;
using Moq;

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
                new object[] { Fixtures.EmptySegment, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentSingleCondition, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentSingleCondition, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 } }, true },
                new object[] { Fixtures.SegmentMultipleConditionsAll, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentMultipleConditionsAll, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 } }, false },
                new object[] { Fixtures.SegmentMultipleConditionsAll, new List<TraitModel> {
                                    new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey2, TraitValue = Fixtures.TraitValue2 }
                                }, true
                },
                new object[] { Fixtures.SegmentMultipleConditionsAny, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentMultipleConditionsAny, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 } }, true },
                new object[] { Fixtures.SegmentMultipleConditionsAny, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey2, TraitValue = Fixtures.TraitValue2 } }, true },
                new object[] { Fixtures.SegmentMultipleConditionsAny, new List<TraitModel> {
                                    new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey2, TraitValue = Fixtures.TraitValue2 }
                                    }, true
                },
                new object[] { Fixtures.SegmentNestedRules, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentNestedRules, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 } }, false },
                new object[] { Fixtures.SegmentNestedRules, new List<TraitModel> {
                                    new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey2, TraitValue = Fixtures.TraitValue2 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey3, TraitValue = Fixtures.TraitValue3 }
                                    }, true
                },
                new object[] { Fixtures.SegmentConditionsAndNestedRules, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentConditionsAndNestedRules, new List<TraitModel> { new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 } }, false },
                new object[] { Fixtures.SegmentConditionsAndNestedRules, new List<TraitModel> {
                                    new TraitModel { TraitKey = Fixtures.TraitKey1, TraitValue = Fixtures.TraitValue1 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey2, TraitValue = Fixtures.TraitValue2 },
                                    new TraitModel { TraitKey = Fixtures.TraitKey3, TraitValue = Fixtures.TraitValue3 }
                                    }, true
                },
                new object[] { Fixtures.SegmentToCheckIfTrait1IsSet, new List<TraitModel>(), false },
                new object[] { Fixtures.SegmentToCheckIfTrait1IsSet, new List<TraitModel>() { new TraitModel {TraitKey = Fixtures.TraitKey1, TraitValue = "foo"}}, true },
                new object[] { Fixtures.SegmentToCheckIfTrait1IsNotSet, new List<TraitModel>() { new TraitModel {TraitKey = Fixtures.TraitKey1, TraitValue = "foo"}}, false },
                new object[] { Fixtures.SegmentToCheckIfTrait1IsNotSet, new List<TraitModel>(), true },
            };
        [Theory]
        [InlineData(10, 1, true)]
        [InlineData(100, 50, true)]
        [InlineData(0, 1, false)]
        [InlineData(10, 20, false)]
        public void TestIdentityInSegmentPercentageSplit(int segmentSplitValue, int identityHashedPercentage, bool expectedResult)
        {
            var percentage_split_condition = new SegmentConditionModel
            {
                Operator = Constants.PercentageSplit,
                Value = segmentSplitValue.ToString()
            };
            var rule = new SegmentRuleModel { Type = Constants.AllRule, Conditions = new List<SegmentConditionModel> { percentage_split_condition } };
            var segment = new SegmentModel { Id = 1, Name = "% split", Rules = new List<SegmentRuleModel> { rule } };
            var hashingMock = new Mock<Hashing>();
            var mockSetup = hashingMock.SetupSequence(p => p.GetHashedPercentageForObjectIds(It.IsAny<List<string>>(), It.IsAny<int>()))
             .Returns(identityHashedPercentage);
            Evaluator.Hashing = hashingMock.Object;
            var result = Evaluator.EvaluateIdentityInSegment(Unit.Fixtures.Identity(), segment, null);
            Assert.Equal(expectedResult, result);
        }
        [Theory]
        [MemberData(nameof(TestCasesIdentities))]
        public void TestIdentityInSegmentPercentageSplitUsesDjangoId(IdentityModel identity, bool expectedResult)
        {
            var percentage_split_condition = new SegmentConditionModel
            {
                Operator = Constants.PercentageSplit,
                Value = "50",
            };
            var rule = new SegmentRuleModel { Type = Constants.AllRule, Conditions = new List<SegmentConditionModel> { percentage_split_condition } };
            var segment = new SegmentModel { Id = 1, Name = "% split", Rules = new List<SegmentRuleModel> { rule } };

            var result = Evaluator.EvaluateIdentityInSegment(identity, segment, null);

            Assert.Equal(expectedResult, result);
        }
        public static IEnumerable<object[]> TestCasesIdentities() =>
            new List<object[]>
            {
                new object[]{new IdentityModel(){ Identifier = "Test", EnvironmentApiKey = "key" }, true},
                new object[]{new IdentityModel(){ DjangoId = 1, Identifier = "Test", EnvironmentApiKey = "key" }, false},
            };
    }
}
