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
            Assert.Equivalent(testCase.Result, result);
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