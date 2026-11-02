using System.Collections.Generic;
using FlagsmithEngine;
using Xunit;

namespace EngineTest.Unit.Segments
{
    public class SegmentConditionTest
    {
        [Theory]
        [InlineData(95, Operator.GreaterThan, "90", true)]
        [InlineData(95, Operator.GreaterThan, "90.5", false)]
        [InlineData(95, Operator.NotEqual, "90.5", false)]
        [InlineData(95, Operator.LessThan, "3000000000", true)]
        [InlineData(95, Operator.Contains, "9", false)]
        [InlineData(95L, Operator.GreaterThan, "90.5", false)]
        [InlineData(95L, Operator.Contains, "9", false)]
        [InlineData(95.5, Operator.GreaterThan, "90", true)]
        [InlineData(95.5, Operator.NotEqual, "foo", false)]
        [InlineData(95.5, Operator.Contains, "9", false)]
        public void TestNumericTraitMatchesSegmentByNumericValue(object traitValue, Operator conditionOperator, string conditionValue, bool expectedMatch)
        {
            // Given
            var context = new EvaluationContext<object, object>
            {
                Environment = new EnvironmentContext { Key = "key", Name = "Environment" },
                Identity = new IdentityContext
                {
                    Identifier = "identity",
                    Traits = new Dictionary<string, object> { { "score", traitValue } },
                },
                Segments = new Dictionary<string, SegmentContext<object, object>>
                {
                    {
                        "1",
                        new SegmentContext<object, object>
                        {
                            Key = "1",
                            Name = "segment",
                            Rules = new[]
                            {
                                new SegmentRule
                                {
                                    Type = TypeEnum.All,
                                    Conditions = new[]
                                    {
                                        new Condition { Operator = conditionOperator, Property = "score", Value = conditionValue },
                                    },
                                },
                            },
                        }
                    },
                },
            };

            // When
            var result = new Engine().GetEvaluationResult(context);

            // Then
            Assert.Equal(expectedMatch, result.Segments.Length == 1);
        }
    }
}
