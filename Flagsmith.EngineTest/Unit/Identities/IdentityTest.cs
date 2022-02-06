using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Identity.Models;
using Xunit;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Trait.Models;
using FlagsmithEngine.Exceptions;
namespace EngineTest.Unit.Identities
{
    public class IdentityTest
    {
        [Fact]
        public void TestCompsiteKey()
        {
            var environmentApiKey = "abc123";
            var identifier = "identity";
            var identity = new IdentityModel { EnvironmentApiKey = environmentApiKey, Identifier = identifier };
            Assert.Equal($"{environmentApiKey}_{identifier}", identity.CompositeKey);
        }
        [Fact]
        public void TestIdentiyModelCreatesDefaultIdentityUUID()
        {
            IdentityModel identity = new IdentityModel { Identifier = "test", EnvironmentApiKey = "some_key" };
            Assert.NotEmpty(identity.IdentityUUID);
        }
        [Fact]
        public void TestGenerateCompositeKey()
        {
            var environmentApiKey = "abc123";
            var identifier = "identity";
            var identity = new IdentityModel();
            Assert.Equal($"{environmentApiKey}_{identifier}", identity.GenerateCompositeKey(environmentApiKey, identifier));
        }
        [Fact]
        public void TestUpdateTraitsRemoveTraitsWithNoneValue()
        {
            IdentityModel identityInSegment = ConfTest.IdentityInSegment();
            var traitKey = identityInSegment.IdentityTraits[0].TraitKey;
            var traitToRemove = new TraitModel { TraitKey = traitKey, TraitValue = null };
            identityInSegment.UpdateTraits(new List<TraitModel> { traitToRemove });
            Assert.Empty(identityInSegment.IdentityTraits);
        }
        [Fact]
        public void TestUpdateIdentityTraitsUpdatesTraitValue()
        {
            IdentityModel identityInSegment = ConfTest.IdentityInSegment();
            var traitKey = identityInSegment.IdentityTraits[0].TraitKey;
            var traitValue = "updated_trait_value";
            var traitToUpdate = new TraitModel { TraitKey = traitKey, TraitValue = traitValue };
            identityInSegment.UpdateTraits(new List<TraitModel> { traitToUpdate });
            Assert.Single(identityInSegment.IdentityTraits);
            Assert.Equal(traitToUpdate, identityInSegment.IdentityTraits[0]);
        }
        [Fact]
        public void TestUpdateTraitsAddsNewTraits()
        {
            IdentityModel identityInSegment = ConfTest.IdentityInSegment();
            var newTrait = new TraitModel { TraitKey = "new_key", TraitValue = "foobar" };
            identityInSegment.UpdateTraits(new List<TraitModel> { newTrait });
            Assert.Equal(2, identityInSegment.IdentityTraits.Count);
            Assert.Contains(newTrait, identityInSegment.IdentityTraits);
        }
        [Fact]
        public void TestAppendingFeatureStatesRaisesDuplicateFeatureStateIfFsForTheFeatureAlreadyExists()
        {
            IdentityModel identity = ConfTest.Identity();
            var fs1 = new FeatureStateModel { Feature = ConfTest.Feature1, Enabled = false };
            var fs2 = new FeatureStateModel { Feature = ConfTest.Feature1, Enabled = true };
            identity.IdentityFeatures = new IdentityFeaturesList { fs1 };
            Assert.Throws<DuplicateFeatureState>(() => identity.IdentityFeatures.Add(fs2));
        }
        [Fact]
        public void TestAppendFeatureState()
        {
            IdentityModel identity = ConfTest.Identity();
            var fs1 = new FeatureStateModel { Feature = ConfTest.Feature1, Enabled = false };
            identity.IdentityFeatures = new IdentityFeaturesList { fs1 };
            identity.IdentityFeatures.Contains(fs1);
        }
    }
}
