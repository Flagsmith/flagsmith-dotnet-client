using FlagsmithEngine.Models;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using FlagsmithEngine.Interfaces;
using System.Linq;

namespace EngineTest
{
    public class EngineTest
    {
        private IEngine _iengine;
        public EngineTest()
        {
            _iengine = new FlagsmithEngine.Engine();
        }
        [Theory]
        [MemberData(nameof(ExtractTestCases), parameters: @"/EngineTestData/data/environment_n9fbf9h3v4fFgH3U3ngWhb.json")]
        public void Test_Engine(EnvironmentModel environmentModel, IdentityModel IdentityModel, Response response)
        {
            var engineResponse = _iengine.GetIdentityFeatureStates(environmentModel, IdentityModel);

            var sortedEngineflags = engineResponse.OrderBy(x => x.Feature.Name).ToList();
            var sortedApiFlags = response.flags.OrderBy(x => x.feature.Name).ToList();

            Assert.Equal(sortedApiFlags.Count(), sortedEngineflags.Count());
            for (int i = 0; i < sortedEngineflags.Count(); i++)
            {
                var valueFromApi = sortedApiFlags[i].feature_state_value?.ToString();
                var valueFromEngine = sortedEngineflags[i].GetValue(IdentityModel.DjangoId?.ToString())?.ToString();

                if (valueFromApi == null)
                {
                    // TODO: this should be Assert.Null but there is an issue in the .NET framework
                    // https://github.com/dotnet/runtime/issues/36510 which is seemingly causing null
                    // values to serialize as empty strings.
                    Assert.True(string.IsNullOrEmpty(valueFromEngine));
                }
                else
                {
                    Assert.Equal(valueFromApi, valueFromEngine);
                }

                Assert.Equal(sortedApiFlags[i].enabled, sortedEngineflags[i].Enabled);
            }
        }
        public static JObject LoadData(string path)
        {
            path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + path;
            using (StreamReader r = new StreamReader(path))
            {
                return JObject.Parse(r.ReadToEnd());
            }
        }
        public static IEnumerable<object[]> ExtractTestCases(string path)
        {
            var testCases = new List<object[]>();
            var testData = LoadData(path);
            var environment_model = testData["environment"].ToObject<EnvironmentModel>();
            foreach (var item in testData["identities_and_responses"])
            {
                var identity_model = item["identity"].ToObject<IdentityModel>();
                var response = item["response"].ToObject<Response>();
                testCases.Add(new object[] { environment_model, identity_model, response });
            }
            return testCases;
        }
    }
}
