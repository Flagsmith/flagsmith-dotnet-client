// AUTO-GENERATED from JSON Schema
//
// Source: https://github.com/Flagsmith/flagsmith/blob/main/sdk/evaluation-result.json
// Generated using: make generate-engine-classes
//
// This file contains types auto-generated from the Flagsmith evaluation result JSON schema.
// The schema is the source of truth for the evaluation result structure.
//
// IMPORTANT: This file has been manually modified to support generic metadata types.
// Do not regenerate without preserving the generic type parameters for metadata.

namespace FlagsmithEngine
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Evaluation result object containing the used context, flag evaluation results, and
    /// segments used in the evaluation.
    /// </summary>
    public partial class EvaluationResult<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Feature flags evaluated for the context, mapped by feature names.
        /// </summary>
        [JsonProperty("flags", Required = Required.Always)]
        public Dictionary<string, FlagResult<FeatureMetadataT>> Flags { get; set; }

        /// <summary>
        /// List of segments which the provided context belongs to.
        /// </summary>
        [JsonProperty("segments", Required = Required.Always)]
        public SegmentResult<SegmentMetadataT>[] Segments { get; set; }
    }

    public partial class FlagResult<FeatureMetadataT>
    {
        /// <summary>
        /// Indicates if the feature flag is enabled.
        /// </summary>
        [JsonProperty("enabled", Required = Required.Always)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Additional metadata associated with the feature.
        /// </summary>
        [JsonProperty("metadata", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public FeatureMetadataT Metadata { get; set; }

        /// <summary>
        /// Feature name.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Reason for the feature flag evaluation.
        /// </summary>
        [JsonProperty("reason", Required = Required.Always)]
        public string Reason { get; set; }

        /// <summary>
        /// Feature flag value.
        /// </summary>
        [JsonProperty("value", Required = Required.AllowNull)]
        public object Value { get; set; }
    }

    public partial class SegmentResult<SegmentMetadataT>
    {
        /// <summary>
        /// Additional metadata associated with the segment.
        /// </summary>
        [JsonProperty("metadata", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public SegmentMetadataT Metadata { get; set; }

        /// <summary>
        /// Segment name.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }
    }
}
