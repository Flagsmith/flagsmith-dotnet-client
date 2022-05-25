using FlagsmithEngine.Feature.Models;
using Xunit;

namespace EngineTest.Unit.Features
{
    public class FeatureStateModelTest
    {
        [Fact]
        public void testFeatureState_IsHigherPriority_TwoNullFeatureSegments() {
            // Given
            FeatureStateModel featureState1 = new FeatureStateModel();
            FeatureStateModel featureState2 = new FeatureStateModel();
            
            // Then
            Assert.False(featureState1.IsHigherPriority(featureState2));
            Assert.False(featureState2.IsHigherPriority(featureState1));
        }

        [Fact]
        public void testFeatureState_IsHigherPriority_OneNullFeatureSegment() {
            // Given
            FeatureStateModel featureState1 = new FeatureStateModel();
            FeatureStateModel featureState2 = new FeatureStateModel();

            FeatureSegmentModel featureSegment = new FeatureSegmentModel();
            featureSegment.Priority = 1;
            featureState1.FeatureSegment = featureSegment;
            
            // Then
            Assert.True(featureState1.IsHigherPriority(featureState2));
            Assert.False(featureState2.IsHigherPriority(featureState1));
        }

        [Fact]
        public void testFeatureState_IsHigherPriority() {
            // Given
            FeatureStateModel featureState1 = new FeatureStateModel();
            FeatureStateModel featureState2 = new FeatureStateModel();

            FeatureSegmentModel featureSegment1 = new FeatureSegmentModel();
            featureSegment1.Priority = 1;
            featureState1.FeatureSegment = featureSegment1;

            FeatureSegmentModel featureSegment2 = new FeatureSegmentModel();
            featureSegment2.Priority = 2;
            featureState2.FeatureSegment = featureSegment2;
            
            // Then
            Assert.True(featureState1.IsHigherPriority(featureState2));
            Assert.False(featureState2.IsHigherPriority(featureState1));
        }
    }
}
