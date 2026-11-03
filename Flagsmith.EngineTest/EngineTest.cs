using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine;
using System.Linq;

namespace EngineTest
{
    public class EngineTest
    {
        private IEngine _iengine;

        private static string TestCasesPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/EngineTestData/test_cases/";

        private struct TestCase
        {
            [JsonProperty("context")]
            public EvaluationContext<object, object> Context { get; set; }
            [JsonProperty("result")]
            public EvaluationResult<object, object> Result { get; set; }
        }

        private static TestCase GetTestCase(string filename)
        {
            var path = TestCasesPath + filename;
            using (StreamReader r = new StreamReader(path))
            {
                return JsonConvert.DeserializeObject<TestCase>(r.ReadToEnd());
            }
        }

        public EngineTest()
        {
            _iengine = new Engine();
        }

        [Theory]
        [MemberData(nameof(ExtractTestCaseFilenames))]
        public void Test_Engine(String testCaseFilename)
        {
            // Given
            var testCase = GetTestCase(testCaseFilename);

            // When
            var result = _iengine.GetEvaluationResult(testCase.Context);

            // Then
            Assert.Equivalent(testCase.Result, result, strict: true);
        }

        public static IEnumerable<object[]> ExtractTestCaseFilenames()
        {
            var testCases = new List<object[]>();
            var testCasePaths = Directory.GetFiles(TestCasesPath, "*.json").Concat(Directory.GetFiles(TestCasesPath, "*.jsonc"));
            foreach (var testCasePath in testCasePaths)
            {
                testCases.Add(new object[] { testCasePath.Replace(TestCasesPath, "") });
            }
            return testCases;
        }

        [Theory]
        [InlineData("$.flags[\"payments\"].enabled")]
        [InlineData("$.flags['payments'].enabled")]
        [InlineData("$.flags.payments.enabled")]
        public void TestGetEvaluationResult_FlagIsDisabledWhileTheFlagItDependsOnIsDisabled(string property)
        {
            // Given
            var context = new EvaluationContext<object, object>
            {
                Environment = new EnvironmentContext { Key = "test-env", Name = "Test Environment" },
                Features = new Dictionary<string, FeatureContext<object>>
                {
                    ["payments"] = new FeatureContext<object> { Key = "1", Name = "payments", Enabled = false },
                    ["checkout"] = new FeatureContext<object> { Key = "2", Name = "checkout", Enabled = true, Value = "v2" },
                },
                Segments = new Dictionary<string, SegmentContext<object, object>>
                {
                    ["1"] = new SegmentContext<object, object>
                    {
                        Key = "1",
                        Name = "payments_disabled",
                        Rules = new[]
                        {
                            new SegmentRule
                            {
                                Type = TypeEnum.All,
                                Conditions = new[] { new Condition { Property = property, Operator = Operator.NotEqual, Value = new ConditionValueUnion { String = "true" } } },
                            },
                        },
                        Overrides = new[] { new FeatureContext<object> { Key = "2", Name = "checkout", Enabled = false, Value = "v2" } },
                    },
                },
            };

            // When
            var result = _iengine.GetEvaluationResult(context);

            // Then
            Assert.False(result.Flags["checkout"].Enabled);
            Assert.Equal("TARGETING_MATCH; segment=payments_disabled", result.Flags["checkout"].Reason);
        }

        [Theory]
        [InlineData("$.plan[")]
        [InlineData("$.environment.name")]
        public void TestGetEvaluationResult_IdentityMatchesSegmentOnTraitNamedLikeJsonPath(string traitKey)
        {
            // Given
            var context = new EvaluationContext<object, object>
            {
                Environment = new EnvironmentContext { Key = "test-env", Name = "Test Environment" },
                Identity = new IdentityContext
                {
                    Identifier = "user-123",
                    Traits = new Dictionary<string, object> { [traitKey] = "premium" },
                },
                Features = new Dictionary<string, FeatureContext<object>>(),
                Segments = new Dictionary<string, SegmentContext<object, object>>
                {
                    ["1"] = new SegmentContext<object, object>
                    {
                        Key = "1",
                        Name = "premium",
                        Rules = new[]
                        {
                            new SegmentRule
                            {
                                Type = TypeEnum.All,
                                Conditions = new[] { new Condition { Property = traitKey, Operator = Operator.Equal, Value = new ConditionValueUnion { String = "premium" } } },
                            },
                        },
                    },
                },
            };

            // When
            var result = _iengine.GetEvaluationResult(context);

            // Then
            Assert.Equal("premium", Assert.Single(result.Segments).Name);
        }

        [Fact]
        public void TestGetEvaluationResult_ShouldNotMutateOriginalContextIdentity()
        {
            // Arrange
            var engine = new Engine();
            var context = new EvaluationContext<object, object>
            {
                Environment = new EnvironmentContext
                {
                    Key = "test-env",
                    Name = "Test Environment"
                },
                Identity = new IdentityContext
                {
                    Identifier = "user-123",
                    Key = null  // Empty Key triggers the clone+mutate logic in GetEnrichedEvaluationContext
                },
                Features = new Dictionary<string, FeatureContext<object>>(),
                Segments = new Dictionary<string, SegmentContext<object, object>>()
            };

            // Act
            var result = engine.GetEvaluationResult(context);

            // Assert: The original context's Identity.Key should still be null
            Assert.Null(context.Identity.Key);

            // ...and the rest of the context should remain unchanged
            Assert.Equal("test-env", context.Environment.Key);
            Assert.Equal("user-123", context.Identity.Identifier);
            Assert.Empty(context.Features);
            Assert.Empty(context.Segments);
        }
    }
}