#nullable enable

namespace Flagsmith
{
    /// <summary>
    /// Metadata associated with a feature in the evaluation context.
    /// </summary>
    public class FeatureMetadata
    {
        /// <summary>
        /// The feature identifier from the API.
        /// </summary>
        public int Id { get; set; }
    }

    /// <summary>
    /// Metadata associated with a segment in the evaluation context.
    /// </summary>
    public class SegmentMetadata
    {
        /// <summary>
        /// The segment identifier from the API. Null for synthetic segments.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The source of the segment: "api" for API-defined segments, "identity_override" for synthetic segments.
        /// </summary>
        public string? Source { get; set; }
    }
}
