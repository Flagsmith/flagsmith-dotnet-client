namespace FlagsmithEngine
{
    using System.Collections.Generic;

    /// <summary>
    /// Evaluation result object containing the used context, flag evaluation results, and
    /// segments used in the evaluation.
    /// </summary>
    public partial class EvaluationResult<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Feature flags evaluated for the context, mapped by feature names.
        /// </summary>
        public Dictionary<string, FlagResult<FeatureMetadataT>> Flags { get; set; }

        /// <summary>
        /// List of segments which the provided context belongs to.
        /// </summary>
        public SegmentResult<SegmentMetadataT>[] Segments { get; set; }
    }

    public partial class FlagResult<MetadataT>
    {
        /// <summary>
        /// Indicates if the feature flag is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Additional metadata associated with the feature.
        /// </summary>
        public MetadataT Metadata { get; set; }

        /// <summary>
        /// Feature name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Reason for the feature flag evaluation.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Feature flag value.
        /// </summary>
        public Value Value { get; set; }
    }

    public partial class SegmentResult<SegmentMetadataT>
    {
        /// <summary>
        /// Additional metadata associated with the segment.
        /// </summary>
        public SegmentMetadataT Metadata { get; set; }

        /// <summary>
        /// Segment name.
        /// </summary>
        public string Name { get; set; }
    }
}
