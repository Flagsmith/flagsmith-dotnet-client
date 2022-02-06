using FlagsmithEngine.Feature;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Utils;
using Moq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EngineTest.Unit.Features
{
    public class FeatureTest
    {
        [Fact]
        public void TestInitializingFeatureStateCreatesDefaultFeatureStateUUID()
        {
            var featureState = new FeatureStateModel { DjangoId = 1, Feature = ConfTest.Feature1, Enabled = true };
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
        [Fact]
        public void TestFeatureStateGetValueNoMvValues()
        {
            var value = "foo";
            var featureState = new FeatureStateModel { DjangoId = 1, Feature = ConfTest.Feature1, Enabled = true };
            featureState.Value = value;
            Assert.Equal(featureState.GetValue(), featureState.GetValue("1"));
            Assert.Equal(featureState.GetValue("1"), value);
        }
        [Theory]
        [InlineData(10, Fixtures.MvFeatureValue1)]
        [InlineData(40, Fixtures.MvFeatureValue2)]
        [InlineData(70, Fixtures.MvFeatureControlValue)]
        public void TestFeatureStateGetValueMvValues(int percentageValue, string expectedValue)
        {
            var myFeature = new FeatureModel { Id = 1, Name = "mv_feature", Type = Constants.STANDARD };
            var mvFeatureOption1 = new MultivariateFeatureOptionModel { Id = 1, Value = Fixtures.MvFeatureValue1 };
            var mvFeatureOption2 = new MultivariateFeatureOptionModel { Id = 2, Value = Fixtures.MvFeatureValue2 };
            var mvFeatureStateValue1 = new MultivariateFeatureStateValueModel { Id = 1, MultivariateFeatureOption = mvFeatureOption1, PercentageAllocation = 30 };
            var mvFeatureStateValue2 = new MultivariateFeatureStateValueModel
            {
                Id = 2,
                MultivariateFeatureOption = mvFeatureOption2,
                PercentageAllocation = 30
            };
            var mvFeatureState = new FeatureStateModel
            {
                DjangoId = 1,
                Feature = myFeature,
                Enabled = true,
                MultivariateFeatureStateValues = new List<MultivariateFeatureStateValueModel>
                {
                    mvFeatureStateValue1,
                    mvFeatureStateValue2,
                }
            };
            mvFeatureState.Value = Fixtures.MvFeatureControlValue;
            var HasingMock = new Mock<Hashing>();
            var mockSetup = HasingMock.SetupSequence(p => p.GetHashedPercentageForObjectIds(It.IsAny<List<string>>(), It.IsAny<int>()))
                .Returns(percentageValue);
            mvFeatureState.Hashing = HasingMock.Object;
            Assert.Equal(expectedValue, mvFeatureState.GetValue("1"));

        }
        [Fact]
        public void TestGetValueUsesDjangoIdForMultivariateValueCalculationIfNotNull()
        {
            var HasingMock = new Mock<Hashing>();
            var mockSetup = HasingMock.SetupSequence(p => p.GetHashedPercentageForObjectIds(It.IsAny<List<string>>(), It.IsAny<int>()))
              .Returns(10);
            var identityID = 1;
            var featureState = new FeatureStateModel
            {
                DjangoId = 1,
                Feature = ConfTest.Feature1,
                Enabled = true,
                MultivariateFeatureStateValues = new List<MultivariateFeatureStateValueModel> { ConfTest.MvFeatureStateValue() },
            };
            featureState.Hashing = HasingMock.Object;
            featureState.GetValue(identityID.ToString());
            var tempIds = new List<string> { featureState.DjangoId.ToString(), identityID.ToString() };
            HasingMock.Verify(h => h.GetHashedPercentageForObjectIds(It.Is<List<string>>(s => s.SequenceEqual(tempIds)), It.IsAny<int>()));
        }
        [Fact]
        public void TestGetValueUsesFeatuestateUUIDForMultivariateValueCalculationIfDjangoIdIsNotPresent()
        {
            var HasingMock = new Mock<Hashing>();
            var mockSetup = HasingMock.SetupSequence(p => p.GetHashedPercentageForObjectIds(It.IsAny<List<string>>(), It.IsAny<int>()))
             .Returns(10);
            var feature1 = ConfTest.Feature1;
            var mVFeatureStateValue = ConfTest.MvFeatureStateValue();
            var identityID = 1;
            var featureState = new FeatureStateModel
            {
                Feature = ConfTest.Feature1,
                Enabled = true,
                MultivariateFeatureStateValues = new List<MultivariateFeatureStateValueModel> { ConfTest.MvFeatureStateValue() },
            };
            featureState.Hashing = HasingMock.Object;
            featureState.GetValue(identityID.ToString());
            var tempIds = new List<string> { featureState.FeatureStateUUID, identityID.ToString() };
            HasingMock.Verify(h => h.GetHashedPercentageForObjectIds(It.Is<List<string>>(s => s.SequenceEqual(tempIds)), It.IsAny<int>()));
        }
    }
}
