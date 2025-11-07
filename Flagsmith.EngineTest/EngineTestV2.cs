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
    public class EngineTestV2
    {
        private IEngine _iengine;

        private static string TestCasesPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/EngineTestDataV2/test_cases/";

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

        public EngineTestV2()
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
    }
}
