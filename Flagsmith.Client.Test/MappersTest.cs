using System.Linq;
using FlagsmithEngine;
using FlagsmithEngine.Segment;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class MappersTest
    {
        [Fact]
        public void MapEnvironmentDocumentToContext_ProducesEvaluationContext()
        {
            // Given
            var environment = Fixtures.Environment;

            // When
            var context = Mappers.MapEnvironmentDocumentToContext(environment);

            // Then
            Assert.IsType<EvaluationContext<SegmentMetadata, FeatureMetadata>>(context);
            Assert.Equal("test_key", context.Environment.Key);
            Assert.Equal("Test Environment", context.Environment.Name);
            Assert.Null(context.Identity);
            Assert.Equal(2, context.Segments.Count);

            // Verify API segment
            Assert.True(context.Segments.ContainsKey("1"));
            var apiSegment = context.Segments["1"];
            Assert.Equal("1", apiSegment.Key);
            Assert.Equal("Test segment", apiSegment.Name);
            Assert.Single(apiSegment.Rules);
            Assert.Empty(apiSegment.Overrides);
            Assert.Equal("api", apiSegment.Metadata.Source);
            Assert.Equal(1, apiSegment.Metadata.Id);

            // Verify segment rule structure
            Assert.Equal(TypeEnum.All, apiSegment.Rules[0].Type);
            Assert.Empty(apiSegment.Rules[0].Conditions);
            Assert.Single(apiSegment.Rules[0].Rules);

            Assert.Equal(TypeEnum.All, apiSegment.Rules[0].Rules[0].Type);
            Assert.Single(apiSegment.Rules[0].Rules[0].Conditions);
            Assert.Empty(apiSegment.Rules[0].Rules[0].Rules);

            Assert.Equal("foo", apiSegment.Rules[0].Rules[0].Conditions[0].Property);
            Assert.Equal(Operator.Equal, apiSegment.Rules[0].Rules[0].Conditions[0].Operator);
            Assert.Equal("bar", apiSegment.Rules[0].Rules[0].Conditions[0].Value.String);

            // Verify identity override segment
            var overrideKey = "42d7556943d3c6f62b310e40f2252ac29203c20f37e9adffd8f12bd084a87b9d";
            Assert.True(context.Segments.ContainsKey(overrideKey));
            var overrideSegment = context.Segments[overrideKey];
            Assert.Equal("", overrideSegment.Key);
            Assert.Equal("identity_overrides", overrideSegment.Name);
            Assert.Single(overrideSegment.Rules);
            Assert.Single(overrideSegment.Overrides);

            Assert.Equal(TypeEnum.All, overrideSegment.Rules[0].Type);
            Assert.Single(overrideSegment.Rules[0].Conditions);
            Assert.Empty(overrideSegment.Rules[0].Rules);

            Assert.Equal("$.identity.identifier", overrideSegment.Rules[0].Conditions[0].Property);
            Assert.Equal(Operator.In, overrideSegment.Rules[0].Conditions[0].Operator);
            Assert.Equal(new[] { "overridden-id" }, overrideSegment.Rules[0].Conditions[0].Value.StringArray);

            Assert.Equal("", overrideSegment.Overrides[0].Key);
            Assert.Equal("some_feature", overrideSegment.Overrides[0].Name);
            Assert.False(overrideSegment.Overrides[0].Enabled);
            Assert.Equal("some-overridden-value", overrideSegment.Overrides[0].Value);
            Assert.Equal(Constants.StrongestPriority, overrideSegment.Overrides[0].Priority);
            Assert.Null(overrideSegment.Overrides[0].Variants);
            Assert.Equal(1, overrideSegment.Overrides[0].Metadata.Id);

            // Verify features
            Assert.Equal(3, context.Features.Count);
            Assert.True(context.Features.ContainsKey("some_feature"));
            var someFeature = context.Features["some_feature"];
            Assert.Equal("00000000-0000-0000-0000-000000000000", someFeature.Key);
            Assert.Equal("some_feature", someFeature.Name);
            Assert.True(someFeature.Enabled);
            Assert.Equal("some-value", someFeature.Value);
            Assert.Null(someFeature.Priority);
            Assert.Empty(someFeature.Variants);
            Assert.Equal(1, someFeature.Metadata.Id);

            // Verify multivariate feature with IDs - priority should be based on ID
            Assert.True(context.Features.ContainsKey("mv_feature_with_ids"));
            var mvFeatureWithIds = context.Features["mv_feature_with_ids"];
            Assert.Equal("2", mvFeatureWithIds.Key);
            Assert.Equal("mv_feature_with_ids", mvFeatureWithIds.Name);
            Assert.True(mvFeatureWithIds.Enabled);
            Assert.Equal("default_value", mvFeatureWithIds.Value);
            Assert.Null(mvFeatureWithIds.Priority);
            Assert.Equal(2, mvFeatureWithIds.Variants.Length);
            Assert.Equal(2, mvFeatureWithIds.Metadata.Id);

            // First variant: ID=100, should have priority 100
            Assert.Equal("variant_a", mvFeatureWithIds.Variants[0].Value);
            Assert.Equal(30.0, mvFeatureWithIds.Variants[0].Weight);
            Assert.Equal(100, mvFeatureWithIds.Variants[0].Priority);

            // Second variant: ID=200, should have priority 200
            Assert.Equal("variant_b", mvFeatureWithIds.Variants[1].Value);
            Assert.Equal(70.0, mvFeatureWithIds.Variants[1].Weight);
            Assert.Equal(200, mvFeatureWithIds.Variants[1].Priority);

            // Verify multivariate feature without IDs - priority should be based on UUID position
            Assert.True(context.Features.ContainsKey("mv_feature_without_ids"));
            var mvFeatureWithoutIds = context.Features["mv_feature_without_ids"];
            Assert.Equal("3", mvFeatureWithoutIds.Key);
            Assert.Equal("mv_feature_without_ids", mvFeatureWithoutIds.Name);
            Assert.False(mvFeatureWithoutIds.Enabled);
            Assert.Equal("fallback_value", mvFeatureWithoutIds.Value);
            Assert.Null(mvFeatureWithoutIds.Priority);
            Assert.Equal(3, mvFeatureWithoutIds.Variants.Length);
            Assert.Equal(3, mvFeatureWithoutIds.Metadata.Id);

            // Variants should be ordered by UUID alphabetically
            Assert.Equal("option_y", mvFeatureWithoutIds.Variants[0].Value);
            Assert.Equal(50.0, mvFeatureWithoutIds.Variants[0].Weight);
            Assert.Equal(1, mvFeatureWithoutIds.Variants[0].Priority); // Second in sorted UUID order

            Assert.Equal("option_x", mvFeatureWithoutIds.Variants[1].Value);
            Assert.Equal(25.0, mvFeatureWithoutIds.Variants[1].Weight);
            Assert.Equal(0, mvFeatureWithoutIds.Variants[1].Priority); // First in sorted UUID order

            Assert.Equal("option_z", mvFeatureWithoutIds.Variants[2].Value);
            Assert.Equal(25.0, mvFeatureWithoutIds.Variants[2].Weight);
            Assert.Equal(2, mvFeatureWithoutIds.Variants[2].Priority); // Third in sorted UUID order
        }
    }
}
