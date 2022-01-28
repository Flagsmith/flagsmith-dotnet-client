using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Feature;
using FlagsmithEngine;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Exceptions;
using Xunit;
using System.Linq;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Segment.Models;
namespace EngineTest.Unit
{
    public class EngineTest
    {
        IEngine _engine = new Engine();
        public void TestIdentityGetFeatureStateWithoutAnyOverride(EnvironmentModel environment, IdentityModel identity, FeatureModel feature)
        {
            var featureState = _engine.GetIdentityFeatureState(environment, identity, feature.Name, null);
            Assert.Equal(featureState.Feature, feature);
        }
        public void TestIdentityGetAllFeatureStatesNoSegments(FeatureModel feature1, FeatureModel feature2, EnvironmentModel environment, IdentityModel identity)
        {
            var overriddenFeature = new FeatureModel { Id = 3, Name = "overridden_feature", Type = Constants.STANDARD };
            environment.FeatureStates.Add(new FeatureStateModel { DjangoId = 3, Feature = overriddenFeature, Enabled = false });
            identity.IdentityFeatures = new IdentityFeaturesList()
            {
                new FeatureStateModel { DjangoId = 4, Feature = overriddenFeature, Enabled = true }
            };
            var allFeatureStates = _engine.GetIdentityFeatureStates(environment, identity);
            Assert.Equal(3, allFeatureStates.Count);
            allFeatureStates.ForEach(f =>
            {
                var envFeature = environment.FeatureStates.Where(fs => fs.Feature == f.Feature);
                var expected = f.Feature == overriddenFeature ? true : f.Enabled;
                Assert.Equal(expected, f.Enabled);
            });
        }
        public void TestGetIdentityFeatureStatesHidesDisabledFlagsIfEnabled(EnvironmentModel environment, IdentityModel identity)
        {
            environment.Project.HideDisabledFlags = true;
            var featureStates = _engine.GetIdentityFeatureStates(environment, identity);
            Assert.Empty(featureStates.Where(f => !f.Enabled));
        }
        public void TestIdentityGetAllFeatureStatesSegmentsOnly(EnvironmentModel environment, SegmentModel segment, IdentityModel identityInSegment)
        {
            var overriddenFeature = new FeatureModel { Id = 3, Name = "overridden_feature", Type = Constants.STANDARD };

            environment.FeatureStates.Add(new FeatureStateModel { DjangoId = 3, Feature = overriddenFeature, Enabled = false });
            segment.FeatureStates.Add(new FeatureStateModel { DjangoId = 4, Feature = overriddenFeature, Enabled = true });
            var AllFeatureStates = _engine.GetIdentityFeatureStates(environment, identityInSegment);
            Assert.Equal(3, AllFeatureStates.Count);
            AllFeatureStates.ForEach(f =>
            {
                var environmentFeatureState = environment.FeatureStates.FirstOrDefault(fs => fs.Feature == f.Feature);
                var expected = f.Feature == overriddenFeature ? true : environmentFeatureState.Enabled;
                Assert.Equal(expected, f.Enabled);
            });
        }
        public void TestIdentityGetAllFeatureStatesWithTraits(EnvironmentModel environmentWithSegmentOverride, IdentityModel identityInSegment)
        {
            var traitModels = new TraitModel
            {
                TraitKey = ConfTest.SegmentConditionProperty,
                TraitValue = ConfTest.SegmentConditionStringValue,
            };
            var allFeatureStates = _engine.GetIdentityFeatureStates(environmentWithSegmentOverride, identityInSegment, new List<TraitModel> { traitModels });
            Assert.Equal("segment_override", allFeatureStates[0].GetValue());
        }
        public void TestEnvironmentGetAllFeatureStates(EnvironmentModel environment)
        {
            var featureStates = _engine.GetEnvironmentFeatureStates(environment);
            Assert.Equal(featureStates, environment.FeatureStates);
        }
        public void TestEnvironmentGetFeatureStatesHidesDisabledFlagsIfEnabled(EnvironmentModel environment)
        {
            environment.Project.HideDisabledFlags = true;
            var featureStates = _engine.GetEnvironmentFeatureStates(environment);
            Assert.NotEqual(featureStates, environment.FeatureStates);
            Assert.Empty(featureStates.Where(f => !f.Enabled));
        }
        public void TestEnvironmentGetFeatureState(EnvironmentModel environment, FeatureModel feature1)
        {
            var featureState = _engine.GetEnvironmentFeatureState(environment, feature1.Name);
            Assert.Equal(feature1, featureState.Feature);
        }
        public void TestEnvironmentGetFeatureStateRaisesFeatureStateNotFound(EnvironmentModel environment)
        {
            Assert.Throws<FeatureStateNotFound>(() => _engine.GetEnvironmentFeatureState(environment, "not_a_feature_name"));
        }
    }
}
