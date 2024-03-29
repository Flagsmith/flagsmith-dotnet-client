﻿using System;
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
        readonly IEngine _engine = new Engine();
        [Fact]
        public void TestIdentityGetFeatureStateWithoutAnyOverride()
        {
            EnvironmentModel environment = Fixtures.Environment();
            IdentityModel identity = Fixtures.Identity();
            FeatureModel feature1 = Fixtures.Feature1;
            var featureState = _engine.GetIdentityFeatureState(environment, identity, feature1.Name, null);
            Assert.Equal(featureState.Feature, feature1);
        }
        [Fact]
        public void TestIdentityGetAllFeatureStatesNoSegments()
        {
            FeatureModel feature1 = Fixtures.Feature1;
            FeatureModel feature2 = Fixtures.Feature2;
            EnvironmentModel environment = Fixtures.Environment();
            IdentityModel identity = Fixtures.Identity();
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
                var expected = f.Feature == overriddenFeature || f.Enabled;
                Assert.Equal(expected, f.Enabled);
            });
        }
        [Fact]
        public void TestGetIdentityFeatureStatesHidesDisabledFlagsIfEnabled()
        {
            EnvironmentModel environment = Fixtures.Environment();
            IdentityModel identity = Fixtures.Identity();
            environment.Project.HideDisabledFlags = true;
            var featureStates = _engine.GetIdentityFeatureStates(environment, identity);
            Assert.Empty(featureStates.Where(f => !f.Enabled));
        }
        [Fact]
        public void TestIdentityGetAllFeatureStatesSegmentsOnly()
        {
            EnvironmentModel environment = Fixtures.Environment();
            IdentityModel identityInSegment = Fixtures.IdentityInSegment();
            SegmentModel segment = Fixtures.Segment;
            var overriddenFeature = new FeatureModel { Id = 3, Name = "overridden_feature", Type = Constants.STANDARD };

            environment.FeatureStates.Add(new FeatureStateModel { DjangoId = 3, Feature = overriddenFeature, Enabled = false });
            segment.FeatureStates = new List<FeatureStateModel> { new FeatureStateModel { DjangoId = 4, Feature = overriddenFeature, Enabled = true } };
            var AllFeatureStates = _engine.GetIdentityFeatureStates(environment, identityInSegment);
            Assert.Equal(3, AllFeatureStates.Count);
            AllFeatureStates.ForEach(f =>
            {
                var environmentFeatureState = environment.FeatureStates.FirstOrDefault(fs => fs.Feature == f.Feature);
                var expected = f.Feature == overriddenFeature || environmentFeatureState.Enabled;
                Assert.Equal(expected, f.Enabled);
            });
        }
        [Fact]
        public void TestIdentityGetAllFeatureStatesWithTraits()
        {
            EnvironmentModel environmentWithSegmentOverride = Fixtures.EnvironmentWithSegmentOverride();
            IdentityModel identityInSegment = Fixtures.IdentityInSegment();
            var traitModels = new TraitModel
            {
                TraitKey = Fixtures.SegmentConditionProperty,
                TraitValue = Fixtures.SegmentConditionStringValue,
            };
            var allFeatureStates = _engine.GetIdentityFeatureStates(environmentWithSegmentOverride, identityInSegment, new List<TraitModel> { traitModels });
            Assert.Equal("segment_override", allFeatureStates[0].GetValue());
        }
        [Fact]
        public void TestEnvironmentGetAllFeatureStates()
        {
            EnvironmentModel environment = Fixtures.Environment();
            var featureStates = _engine.GetEnvironmentFeatureStates(environment);
            Assert.Equal(featureStates, environment.FeatureStates);
        }
        [Fact]
        public void TestEnvironmentGetFeatureStatesHidesDisabledFlagsIfEnabled()
        {
            EnvironmentModel environment = Fixtures.Environment();
            environment.Project.HideDisabledFlags = true;
            var featureStates = _engine.GetEnvironmentFeatureStates(environment);
            Assert.False(environment.FeatureStates == featureStates);
            Assert.Empty(featureStates.Where(f => !f.Enabled));
        }
        [Fact]
        public void TestEnvironmentGetFeatureState()
        {
            EnvironmentModel environment = Fixtures.Environment();
            FeatureModel feature1 = Fixtures.Feature1;
            var featureState = _engine.GetEnvironmentFeatureState(environment, feature1.Name);
            Assert.Equal(feature1, featureState.Feature);
        }
        [Fact]
        public void TestEnvironmentGetFeatureStateRaisesFeatureStateNotFound()
        {
            EnvironmentModel environment = Fixtures.Environment();
            Assert.Throws<FeatureStateNotFound>(() => _engine.GetEnvironmentFeatureState(environment, "not_a_feature_name"));
        }
    }
}
