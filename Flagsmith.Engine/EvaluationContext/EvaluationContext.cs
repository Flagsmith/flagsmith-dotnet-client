namespace FlagsmithEngine
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using FlagsmithEngine.Segment;

    /// <summary>
    /// A context object containing the necessary information to evaluate Flagsmith feature flags.
    /// </summary>
    public partial class EvaluationContext<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Environment context required for evaluation.
        /// </summary>
        [JsonProperty("environment", Required = Required.Always)]
        public EnvironmentContext Environment { get; set; }

        /// <summary>
        /// Features to be evaluated in the context.
        /// </summary>
        [JsonProperty("features", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, FeatureContext<FeatureMetadataT>> Features { get; set; }

        /// <summary>
        /// Identity context used for identity-based evaluation.
        /// </summary>
        [JsonProperty("identity")]
        public IdentityContext Identity { get; set; }

        /// <summary>
        /// Segments applicable to the evaluation context.
        /// </summary>
        [JsonProperty("segments", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, SegmentContext<SegmentMetadataT, FeatureMetadataT>> Segments { get; set; }


        /// <summary>
        /// Creates a copy of the EvaluationContext object
        /// for internal use in the engine.
        /// Optimised to avoid deep cloning where possible.
        /// </summary>
        /// <returns>EvaluationContext</returns>
        public EvaluationContext<SegmentMetadataT, FeatureMetadataT> Clone()
        {
            var clone = (EvaluationContext<SegmentMetadataT, FeatureMetadataT>)MemberwiseClone();
            clone.Identity = clone.Identity?.Clone();
            return clone;
        }
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
        [JsonProperty("key", Required = Required.Always)]
        public string Key { get; set; }

        /// <summary>
        /// An environment's human-readable name.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a feature context for feature flag evaluation.
    /// </summary>
    public partial class FeatureContext<FeatureMetadataT>
    {
        /// <summary>
        /// Indicates whether the feature is enabled in the environment.
        /// </summary>
        [JsonProperty("enabled", Required = Required.Always)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Unique feature key used when selecting a variant if the feature is multivariate. Set to
        /// an internal identifier or a UUID, depending on Flagsmith implementation.
        /// </summary>
        [JsonProperty("key", Required = Required.Always)]
        public string Key { get; set; }

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
        /// Priority of the feature context. Lower values indicate a higher priority when multiple
        /// contexts apply to the same feature.
        /// </summary>
        [JsonProperty("priority", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public double? Priority { get; set; }

        /// <summary>
        /// A default environment value for the feature. If the feature is multivariate, this will be
        /// the control value.
        /// </summary>
        [JsonProperty("value", Required = Required.AllowNull)]
        public object Value { get; set; }

        /// <summary>
        /// An array of environment default values associated with the feature. Empty for standard
        /// features, or contains multiple values for multivariate features.
        /// </summary>
        [JsonProperty("variants", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
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
        [JsonProperty("priority", Required = Required.Always)]
        public double Priority { get; set; }

        /// <summary>
        /// The value of the feature.
        /// </summary>
        [JsonProperty("value", Required = Required.AllowNull)]
        public object Value { get; set; }

        /// <summary>
        /// The weight of the feature value variant, as a percentage number (i.e. 100.0).
        /// </summary>
        [JsonProperty("weight", Required = Required.Always)]
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
        [JsonProperty("identifier", Required = Required.Always)]
        public string Identifier { get; set; }

        /// <summary>
        /// Key used when selecting a value for a multivariate feature, or for % split segmentation.
        /// Set to an internal identifier or a composite value based on the environment key and
        /// identifier, depending on Flagsmith implementation.
        /// </summary>
        [JsonProperty("key", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; set; }

        /// <summary>
        /// A map of traits associated with the identity, where the key is the trait name and the
        /// value is the trait value.
        /// </summary>
        [JsonProperty("traits", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Traits { get; set; }

        /// <summary>
        /// Creates a copy of the IdentityContext object
        /// for internal use in the engine.
        /// </summary>
        /// <returns>IdentityContext</returns>
        public IdentityContext Clone()
        {
            return (IdentityContext)MemberwiseClone();
        }
    }

    /// <summary>
    /// Represents a segment context for feature flag evaluation.
    /// </summary>
    public partial class SegmentContext<SegmentMetadataT, FeatureMetadataT>
    {
        /// <summary>
        /// Unique segment key used for % split segmentation.
        /// </summary>
        [JsonProperty("key", Required = Required.Always)]
        public string Key { get; set; }

        /// <summary>
        /// Additional metadata associated with the segment.
        /// </summary>
        [JsonProperty("metadata", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public SegmentMetadataT Metadata { get; set; }

        /// <summary>
        /// The name of the segment.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Feature overrides for the segment.
        /// </summary>
        [JsonProperty("overrides", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public FeatureContext<FeatureMetadataT>[] Overrides { get; set; }

        /// <summary>
        /// Rules that define the segment.
        /// </summary>
        [JsonProperty("rules", Required = Required.Always)]
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
        [JsonProperty("conditions", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public Condition[] Conditions { get; set; }

        /// <summary>
        /// Sub-rules nested within the segment rule.
        /// </summary>
        [JsonProperty("rules", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public SegmentRule[] Rules { get; set; }

        /// <summary>
        /// Segment rule type. Represents a logical quantifier for the conditions and sub-rules.
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }
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
        [JsonProperty("operator", Required = Required.Always)]
        public string Operator { get; set; }

        /// <summary>
        /// A reference to the identity trait or value in the evaluation context.
        /// </summary>
        [JsonProperty("property", Required = Required.Always)]
        public string Property { get; set; }

        /// <summary>
        /// The value to compare against the trait or context value.
        ///
        /// The values to compare against the trait or context value.
        /// </summary>
        [JsonConverter(typeof(ConditionValueUnionConverter))]
        [JsonProperty("value", Required = Required.Always)]
        public ConditionValueUnion Value { get; set; }
    }



    public partial struct ConditionValueUnion
    {
        public string String;
        public string[] StringArray;

        public static implicit operator ConditionValueUnion(string String) => new ConditionValueUnion { String = String };
        public static implicit operator ConditionValueUnion(string[] StringArray) => new ConditionValueUnion { StringArray = StringArray };
    }



    internal class ConditionValueUnionConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(ConditionValueUnion) || t == typeof(ConditionValueUnion?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                case JsonToken.Date:
                    var stringValue = serializer.Deserialize<string>(reader);
                    return new ConditionValueUnion { String = stringValue };
                case JsonToken.StartArray:
                    var arrayValue = serializer.Deserialize<string[]>(reader);
                    return new ConditionValueUnion { StringArray = arrayValue };
            }
            throw new Exception("Cannot unmarshal type ConditionValueUnion");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            var value = (ConditionValueUnion)untypedValue;
            if (value.String != null)
            {
                serializer.Serialize(writer, value.String);
                return;
            }
            if (value.StringArray != null)
            {
                serializer.Serialize(writer, value.StringArray);
                return;
            }
            throw new Exception("Cannot marshal type ConditionValueUnion");
        }

        public static readonly ConditionValueUnionConverter Singleton = new ConditionValueUnionConverter();
    }


}
