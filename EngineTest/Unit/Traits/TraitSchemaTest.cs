using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Newtonsoft.Json.Linq;
using FlagsmithEngine.Trait.Models;
namespace EngineTest.Unit.Traits
{
    public class TraitSchemaTest
    {
        [Theory]
        [InlineData(@"{ 'trait_key': 'key', 'trait_value': 'value' }")]
        [InlineData(@"{ 'trait_key': 'key1', 'trait_value': 21 }")]
        [InlineData(@"{ 'trait_key': 'key1', 'trait_value': null }")]
        [InlineData(@"{ 'trait_key': 'key1', 'trait_value': 11.2 }")]
        [InlineData(@"{ 'trait_key': 'key1', 'trait_value': true }")]

        public void TestTraitSchemaLoad(string json)
        {
            var jObject = JObject.Parse(json);
            var trait = jObject.ToObject<TraitModel>();
            Assert.Equal(jObject["trait_key"].Value<string>(), trait.TraitKey);
            var valueToken = jObject["trait_value"];
            Assert.Equal(valueToken.Type != JTokenType.Null ? valueToken.Value<object>() : null, trait.TraitValue);
        }
        [Theory]
        [MemberData(nameof(TestTraitToJobjectData))]
        public void TestTraitToJobject(TraitModel trait)
        {
            var jObject = JObject.FromObject(trait);
            Assert.Equal(trait.TraitKey, jObject["trait_key"].Value<string>());
            var valueToken = jObject["trait_value"];
            var value = valueToken.Type != JTokenType.Null ? Convert.ChangeType(valueToken.Value<string>(), trait.TraitValue.GetType()) : null;
            Assert.Equal(trait.TraitValue, value);
        }
        public static IEnumerable<object[]> TestTraitToJobjectData() =>
            new List<object[]>
            {
                new object[]
                {
                    new TraitModel{ TraitKey = "key", TraitValue = "value" },
                },
                new object[]
                {
                    new TraitModel{ TraitKey = "key1", TraitValue = 21},
                },new object[]
                {
                    new TraitModel{ TraitKey = "key1", TraitValue = null },
                },
                new object[]
                {
                    new TraitModel{ TraitKey = "key1", TraitValue = 11.2 },
                },
                new object[]
                {
                    new TraitModel{ TraitKey = "key1", TraitValue = true },
                }
            };


    }
}
