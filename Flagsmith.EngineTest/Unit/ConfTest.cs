using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Organization.Models;
using FlagsmithEngine.Project.Models;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineTest.Unit
{
    public class ConfTest
    {
        public const string SegmentConditionProperty = "foo";
        public const string SegmentConditionStringValue = "bar";
        public static FeatureModel Feature1 { get; } = new FeatureModel { Id = 1, Name = "feature_1", Type = FlagsmithEngine.Feature.Constants.STANDARD };
        public static FeatureModel Feature2 { get; } = new FeatureModel { Id = 1, Name = "feature_2", Type = FlagsmithEngine.Feature.Constants.STANDARD };
        public static SegmentModel Segment { get; } = new SegmentModel { Id = 1, Name = "my_segment", Rules = new List<SegmentRuleModel> { SegmentRule() } };

        public static SegmentConditionModel SegmentCondition() => new SegmentConditionModel { Operator = Constants.Equal, Property = SegmentConditionProperty, Value = SegmentConditionStringValue };
        public static SegmentRuleModel SegmentRule() =>
                new SegmentRuleModel { Type = Constants.AllRule, Conditions = new List<SegmentConditionModel> { SegmentCondition() } };
        public static OrganizationModel Organization() =>
            new OrganizationModel { Id = 1, Name = "test Org", StopServingFlags = false, PersistTraitData = true, FeatureAnalytics = true, };
        public static ProjectModel Project() => new ProjectModel { Id = 1, Name = "Test Project", Organization = Organization(), HideDisabledFlags = false, Segments = new List<SegmentModel> { Segment } };
        public static EnvironmentModel Environment() => new EnvironmentModel
        {
            ID = 1,
            ApiKey = "api-key",
            Project = Project(),
            FeatureStates = new List<FeatureStateModel>
            {
                new FeatureStateModel{ DjangoId = 1, Feature = Feature1, Enabled = true },
                new FeatureStateModel{ DjangoId = 2, Feature = Feature2, Enabled = true },
            }
        };
        public static IdentityModel Identity() => new IdentityModel { Identifier = "identity_1", EnvironmentApiKey = Environment().ApiKey, CreatedDate = DateTime.Now };
        public static TraitModel TraitMatchingSegment()
        {
            var segmentCondtion = SegmentCondition();
            return new TraitModel { TraitKey = segmentCondtion.Property, TraitValue = segmentCondtion.Value };
        }
        public static IdentityModel IdentityInSegment() => new IdentityModel { Identifier = "identity_2", EnvironmentApiKey = Environment().ApiKey, IdentityTraits = new List<TraitModel> { TraitMatchingSegment() } };
        public static FeatureStateModel SegmentOverrideFs() => new FeatureStateModel { DjangoId = 4, Feature = Feature1, Enabled = false, Value = "segment_override" };
        public static MultivariateFeatureStateValueModel MvFeatureStateValue() =>
            new MultivariateFeatureStateValueModel { Id = 1, MultivariateFeatureOption = new MultivariateFeatureOptionModel { Id = 1, Value = "test_value" }, PercentageAllocation = 100 };
        public static EnvironmentModel EnvironmentWithSegmentOverride()
        {
            var segment = Segment;
            segment.FeatureStates = new List<FeatureStateModel> { SegmentOverrideFs() };
            var environemnt = Environment();
            environemnt.Project.Segments.Add(segment);
            return environemnt;
        }
    }
}
