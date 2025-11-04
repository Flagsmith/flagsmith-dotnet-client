using OneOf;

namespace FlagsmithEngine
{
    using System.Collections.Generic;
    using OneOf.Types;

    /// <summary>
    /// A context object containing the necessary information to evaluate Flagsmith feature flags.
    /// </summary>
    public partial class EvaluationContext<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Environment context required for evaluation.
        /// </summary>
        public EnvironmentContext Environment { get; set; }

        /// <summary>
        /// Features to be evaluated in the context.
        /// </summary>
        public Dictionary<string, FeatureContext<FeatureMetadataT>> Features { get; set; }

        /// <summary>
        /// Identity context used for identity-based evaluation.
        /// </summary>
        public IdentityContext Identity { get; set; }

        /// <summary>
        /// Segments applicable to the evaluation context.
        /// </summary>
        public Dictionary<string, SegmentContext<SegmentMetadataT, FeatureMetadataT>> Segments { get; set; }
    }

    /// <summary>
    /// Environment context required for evaluation.
    ///
    /// Represents an environment context for feature flag evaluation.
    /// </summary>
    public partial class EnvironmentContext
    {
        /// <summary>
        /// Unique environment key. May be used for selecting a value for a multivariate feature, or
        /// for % split segmentation.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// An environment's human-readable name.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a feature context for feature flag evaluation.
    /// </summary>
    public partial class FeatureContext<MetadataT>
    {
        /// <summary>
        /// Indicates whether the feature is enabled in the environment.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Unique feature key used when selecting a variant if the feature is multivariate. Set to
        /// an internal identifier or a UUID, depending on Flagsmith implementation.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Additional metadata associated with the feature.
        /// </summary>
        public MetadataT Metadata { get; set; }

        /// <summary>
        /// Feature name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Priority of the feature context. Lower values indicate a higher priority when multiple
        /// contexts apply to the same feature.
        /// </summary>
        public double? Priority { get; set; }

        /// <summary>
        /// A default environment value for the feature. If the feature is multivariate, this will be
        /// the control value.
        /// </summary>
        public OneOf<None, bool, double, int, string> Value { get; set; }

        /// <summary>
        /// An array of environment default values associated with the feature. Empty for standard
        /// features, or contains multiple values for multivariate features.
        /// </summary>
        public FeatureValue[] Variants { get; set; }
    }

    /// <summary>
    /// Represents a multivariate value for a feature flag.
    /// </summary>
    public partial class FeatureValue
    {
        /// <summary>
        /// Priority of the feature flag variant. Lower values indicate a higher priority when
        /// multiple variants apply to the same context key.
        /// </summary>
        public double Priority { get; set; }

        /// <summary>
        /// The value of the feature.
        /// </summary>
        public OneOf<None, bool, double, int, string> Value { get; set; }

        /// <summary>
        /// The weight of the feature value variant, as a percentage number (i.e. 100.0).
        /// </summary>
        public double Weight { get; set; }
    }

    /// <summary>
    /// Represents an identity context for feature flag evaluation.
    /// </summary>
    public partial class IdentityContext
    {
        /// <summary>
        /// A unique identifier for an identity as displayed in the Flagsmith UI.
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Key used when selecting a value for a multivariate feature, or for % split segmentation.
        /// Set to an internal identifier or a composite value based on the environment key and
        /// identifier, depending on Flagsmith implementation.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// A map of traits associated with the identity, where the key is the trait name and the
        /// value is the trait value.
        /// </summary>
        public Dictionary<string, OneOf<None, bool, double, int, string>> Traits { get; set; }
    }

    /// <summary>
    /// Represents a segment context for feature flag evaluation.
    /// </summary>
    public partial class SegmentContext<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Unique segment key used for % split segmentation.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Additional metadata associated with the segment.
        /// </summary>
        public SegmentMetadataT Metadata { get; set; }

        /// <summary>
        /// The name of the segment.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Feature overrides for the segment.
        /// </summary>
        public FeatureContext<FeatureMetadataT>[] Overrides { get; set; }

        /// <summary>
        /// Rules that define the segment.
        /// </summary>
        public SegmentRule[] Rules { get; set; }
    }

    /// <summary>
    /// Represents a rule within a segment for feature flag evaluation.
    /// </summary>
    public partial class SegmentRule
    {
        /// <summary>
        /// Conditions that must be met for the rule to apply.
        /// </summary>
        public Condition[] Conditions { get; set; }

        /// <summary>
        /// Sub-rules nested within the segment rule.
        /// </summary>
        public SegmentRule[] Rules { get; set; }

        /// <summary>
        /// Segment rule type. Represents a logical quantifier for the conditions and sub-rules.
        /// </summary>
        public TypeEnum Type { get; set; }
    }

    /// <summary>
    /// Represents a condition within a segment rule for feature flag evaluation.
    ///
    /// Represents an IN condition within a segment rule for feature flag evaluation.
    /// </summary>
    public partial class Condition
    {
        /// <summary>
        /// The operator to use for evaluating the condition.
        /// </summary>
        public Operator Operator { get; set; }

        /// <summary>
        /// A reference to the identity trait or value in the evaluation context.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The value to compare against the trait or context value.
        ///
        /// The values to compare against the trait or context value.
        /// </summary>
        public ConditionValueUnion Value { get; set; }
    }

    /// <summary>
    /// The operator to use for evaluating the condition.
    /// </summary>
    public enum Operator { Contains, Equal, GreaterThan, GreaterThanInclusive, In, IsNotSet, IsSet, LessThan, LessThanInclusive, Modulo, NotContains, NotEqual, PercentageSplit, Regex };

    /// <summary>
    /// Segment rule type. Represents a logical quantifier for the conditions and sub-rules.
    /// </summary>
    public enum TypeEnum { All, Any, None };

    public partial struct ConditionValueUnion
    {
        public string String;
        public string[] StringArray;

        public static implicit operator ConditionValueUnion(string String) => new ConditionValueUnion { String = String };
        public static implicit operator ConditionValueUnion(string[] StringArray) => new ConditionValueUnion { StringArray = StringArray };
    }
}
