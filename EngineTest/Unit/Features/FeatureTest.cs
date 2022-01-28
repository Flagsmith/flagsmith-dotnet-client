using FlagsmithEngine.Feature.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EngineTest.Unit.Features
{
    public class FeatureTest
    {
        public void TestInitializingFeatureStateCreatesDefaultFeatureStateUUID(FeatureModel feature1)
        {
            var featureState = new FeatureStateModel { DjangoId = 1, Feature = feature1, Enabled = true };
            Assert.NotNull(featureState.FeatureStateUUID);
        }
        [Fact]
        public void TestInitializingMultivariateFeatureStateValueCreatesDefaultUUID()
        {
            var mvFsValueModel = new MultivariateFeatureStateValueModel
            {
                MultivariateFeatureOption = new MultivariateFeatureOptionModel { Value = "value" },
                Id = 1,
                PercentageAllocation = 10
            };
            Assert.NotNull(mvFsValueModel.MvFsValueUUID);
        }
        public void TestFeatureStateGetValueNoMvValues(FeatureModel feature1)
        {
            var value = "foo";
            var featureState = new FeatureStateModel { DjangoId = 1, Feature = feature1, Enabled = true };
            featureState.Value = value;
            Assert.Equal(featureState.GetValue(), featureState.GetValue("1"));
            Assert.Equal(featureState.GetValue("1"), value);
        }
        public void TestFeatureStateGetValueMvValues() { }
        public void TestGetValueUsesDjangoIdForMultivariateValueCalculationIfNotNull() { }
        public void TestGetValueUsesFeatuestateUUIDForMultivariateValueCalculationIfDjangoIdIsNotPresent() { }
    }
}
