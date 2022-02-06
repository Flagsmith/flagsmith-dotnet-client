using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Feature.Models;
using Xunit;
using Newtonsoft.Json.Linq;
using FlagsmithEngine.Exceptions;
namespace EngineTest.Unit.Features
{
    public class FeatureSchemaTest
    {
        [Fact]
        public void TestCanLoadMultivariateFeatureOptionDictWithoutIdField()
        {
            new MultivariateFeatureOptionModel { Value = "" };
            Assert.True(true);
        }
        [Fact]
        public void TestCanLoadMultivariateFeatureStateValueWithoutIdField()
        {
            JObject.Parse(@"{
            'multivariate_feature_option': {'value': 1},
            'percentage_allocation': 10,
        }").ToObject<MultivariateFeatureStateValueModel>();
            Assert.True(true);
        }
        [Fact]
        public void TestDumpingFsSchemaRaisesInvalidPercentageAllocationForInvalidAllocation()
        {
            var feature = new FeatureStateModel
            {
                MultivariateFeatureStateValues = new List<MultivariateFeatureStateValueModel>
                {
                    new MultivariateFeatureStateValueModel
                    {
                        MultivariateFeatureOption = new MultivariateFeatureOptionModel {Value = 12},
                        PercentageAllocation = 100
                    },
                    new MultivariateFeatureStateValueModel
                    {
                        MultivariateFeatureOption = new MultivariateFeatureOptionModel {Value = 9},
                        PercentageAllocation = 80
                    }
                }
            };
            var ex = Assert.ThrowsAny<Exception>(() => JObject.FromObject(feature).ToString());
            Assert.IsType<InvalidPercentageAllocation>(ex.InnerException);
        }
        [Fact]
        public void TestDumpingFsSchemaWorksForValidAllocation()
        {
            var feature = new FeatureStateModel
            {
                MultivariateFeatureStateValues = new List<MultivariateFeatureStateValueModel>
                {
                    new MultivariateFeatureStateValueModel
                    {
                        MultivariateFeatureOption = new MultivariateFeatureOptionModel {Value = 12},
                        PercentageAllocation = 20
                    },
                    new MultivariateFeatureStateValueModel
                    {
                        MultivariateFeatureOption = new MultivariateFeatureOptionModel {Value = 9},
                        PercentageAllocation = 80
                    }
                }
            };
            JObject.FromObject(feature).ToString();
            Assert.True(true);
        }
    }
}
