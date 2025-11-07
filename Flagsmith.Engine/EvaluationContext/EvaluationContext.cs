namespace FlagsmithEngine
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

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

        public EvaluationContext<SegmentMetadataT, FeatureMetadataT> Clone()
        {
            return (EvaluationContext<SegmentMetadataT, FeatureMetadataT>)MemberwiseClone();
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
        [JsonProperty("operator", Required = Required.Always)]
        public Operator Operator { get; set; }

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

    /// <summary>
    /// The operator to use for evaluating the condition.
    /// </summary>
    [JsonConverter(typeof(OperatorConverter))]
    public enum Operator { Contains, Equal, GreaterThan, GreaterThanInclusive, In, IsNotSet, IsSet, LessThan, LessThanInclusive, Modulo, NotContains, NotEqual, PercentageSplit, Regex };

    /// <summary>
    /// Segment rule type. Represents a logical quantifier for the conditions and sub-rules.
    /// </summary>
    [JsonConverter(typeof(TypeEnumConverter))]
    public enum TypeEnum { All, Any, None };

    public partial struct ConditionValueUnion
    {
        public string String;
        public string[] StringArray;

        public static implicit operator ConditionValueUnion(string String) => new ConditionValueUnion { String = String };
        public static implicit operator ConditionValueUnion(string[] StringArray) => new ConditionValueUnion { StringArray = StringArray };
    }

    internal class OperatorConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Operator) || t == typeof(Operator?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "CONTAINS":
                    return Operator.Contains;
                case "EQUAL":
                    return Operator.Equal;
                case "GREATER_THAN":
                    return Operator.GreaterThan;
                case "GREATER_THAN_INCLUSIVE":
                    return Operator.GreaterThanInclusive;
                case "IN":
                    return Operator.In;
                case "IS_NOT_SET":
                    return Operator.IsNotSet;
                case "IS_SET":
                    return Operator.IsSet;
                case "LESS_THAN":
                    return Operator.LessThan;
                case "LESS_THAN_INCLUSIVE":
                    return Operator.LessThanInclusive;
                case "MODULO":
                    return Operator.Modulo;
                case "NOT_CONTAINS":
                    return Operator.NotContains;
                case "NOT_EQUAL":
                    return Operator.NotEqual;
                case "PERCENTAGE_SPLIT":
                    return Operator.PercentageSplit;
                case "REGEX":
                    return Operator.Regex;
            }
            throw new Exception("Cannot unmarshal type Operator");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Operator)untypedValue;
            switch (value)
            {
                case Operator.Contains:
                    serializer.Serialize(writer, "CONTAINS");
                    return;
                case Operator.Equal:
                    serializer.Serialize(writer, "EQUAL");
                    return;
                case Operator.GreaterThan:
                    serializer.Serialize(writer, "GREATER_THAN");
                    return;
                case Operator.GreaterThanInclusive:
                    serializer.Serialize(writer, "GREATER_THAN_INCLUSIVE");
                    return;
                case Operator.In:
                    serializer.Serialize(writer, "IN");
                    return;
                case Operator.IsNotSet:
                    serializer.Serialize(writer, "IS_NOT_SET");
                    return;
                case Operator.IsSet:
                    serializer.Serialize(writer, "IS_SET");
                    return;
                case Operator.LessThan:
                    serializer.Serialize(writer, "LESS_THAN");
                    return;
                case Operator.LessThanInclusive:
                    serializer.Serialize(writer, "LESS_THAN_INCLUSIVE");
                    return;
                case Operator.Modulo:
                    serializer.Serialize(writer, "MODULO");
                    return;
                case Operator.NotContains:
                    serializer.Serialize(writer, "NOT_CONTAINS");
                    return;
                case Operator.NotEqual:
                    serializer.Serialize(writer, "NOT_EQUAL");
                    return;
                case Operator.PercentageSplit:
                    serializer.Serialize(writer, "PERCENTAGE_SPLIT");
                    return;
                case Operator.Regex:
                    serializer.Serialize(writer, "REGEX");
                    return;
            }
            throw new Exception("Cannot marshal type Operator");
        }

        public static readonly OperatorConverter Singleton = new OperatorConverter();
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

    internal class TypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "ALL":
                    return TypeEnum.All;
                case "ANY":
                    return TypeEnum.Any;
                case "NONE":
                    return TypeEnum.None;
            }
            throw new Exception("Cannot unmarshal type TypeEnum");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeEnum)untypedValue;
            switch (value)
            {
                case TypeEnum.All:
                    serializer.Serialize(writer, "ALL");
                    return;
                case TypeEnum.Any:
                    serializer.Serialize(writer, "ANY");
                    return;
                case TypeEnum.None:
                    serializer.Serialize(writer, "NONE");
                    return;
            }
            throw new Exception("Cannot marshal type TypeEnum");
        }

        public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
    }
}
