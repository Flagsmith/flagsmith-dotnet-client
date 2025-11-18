using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using FlagsmithEngine;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;

namespace Flagsmith
{
    /// <summary>
    /// Utility class for transforming environment documents into evaluation contexts.
    /// </summary>
    public static class Mappers
    {
        /// <summary>
        /// Property value for operators that do not use property (e.g., PERCENTAGE_SPLIT).
        /// </summary>
        private const string EmptyProperty = "";

        /// <summary>
        /// Key value for synthetic segments and identity override features.
        /// </summary>
        private const string UnusedKey = "";

        /// <summary>
        /// Parse the environment document into an EvaluationContext object
        /// </summary>
        public static EvaluationContext<SegmentMetadata, FeatureMetadata> MapEnvironmentDocumentToContext(
            EnvironmentModel environmentDocument)
        {
            var context = new EvaluationContext<SegmentMetadata, FeatureMetadata>
            {
                Environment = new EnvironmentContext
                {
                    Key = environmentDocument.ApiKey,
                    Name = environmentDocument.Project.Name,
                },
                Segments = new Dictionary<string, SegmentContext<SegmentMetadata, FeatureMetadata>>()
            };

            if (environmentDocument.Project.Segments != null)
            {
                foreach (var srcSegment in environmentDocument.Project.Segments)
                {
                    var segment = new SegmentContext<SegmentMetadata, FeatureMetadata>
                    {
                        Key = srcSegment.Id.ToString(),
                        Name = srcSegment.Name,
                        Rules = MapEnvironmentDocumentRulesToContextRules(srcSegment.Rules),
                        Metadata = new SegmentMetadata
                        {
                            Source = "api",
                            Id = srcSegment.Id,
                        },
                    };

                    var overrides = MapEnvironmentDocumentFeatureStatesToFeatureContexts(srcSegment.FeatureStates);
                    segment.Overrides = overrides.Values.ToArray();

                    context.Segments[segment.Key] = segment;
                }
            }

            if (environmentDocument.IdentityOverrides != null)
            {
                var identityOverrideSegments = MapIdentityOverridesToSegments(environmentDocument.IdentityOverrides);
                foreach (var kvp in identityOverrideSegments)
                {
                    context.Segments[kvp.Key] = kvp.Value;
                }
            }

            context.Features = MapEnvironmentDocumentFeatureStatesToFeatureContexts(environmentDocument.FeatureStates);

            return context;
        }

        /// <summary>
        /// Attaches identity context into a new evaluation context based on context
        /// </summary>
        public static EvaluationContext<SegmentMetadata, FeatureMetadata> MapContextAndIdentityToContext(
            EvaluationContext<SegmentMetadata, FeatureMetadata> context,
            string identifier,
            List<ITrait> traits)
        {
            var identity = new IdentityContext
            {
                Identifier = identifier,
                Traits = traits?.ToDictionary(t => t.GetTraitKey(), t => t.GetTraitValue()) ?? new Dictionary<string, object>(),
            };

            context = context.Clone();
            context.Identity = identity;
            return context;
        }

        private static SegmentRule[] MapEnvironmentDocumentRulesToContextRules(List<SegmentRuleModel> srcRules)
        {
            return srcRules.Select(srcRule => new SegmentRule
            {
                Type = MapRuleType(srcRule.Type),
                Conditions = (srcRule.Conditions ?? Enumerable.Empty<SegmentConditionModel>())
                    .Select(c => new Condition
                    {
                        Property = c.Property ?? EmptyProperty,
                        Operator = MapOperator(c.Operator),
                        Value = MapConditionValue(c.Value),
                    })
                    .ToArray(),
                Rules = srcRule.Rules != null
                    ? MapEnvironmentDocumentRulesToContextRules(srcRule.Rules)
                    : Array.Empty<SegmentRule>(),
            }).ToArray();
        }

        private static Dictionary<string, FeatureContext<FeatureMetadata>> MapEnvironmentDocumentFeatureStatesToFeatureContexts(
            List<FeatureStateModel> featureStates)
        {
            var featureContexts = new Dictionary<string, FeatureContext<FeatureMetadata>>();

            foreach (var featureState in featureStates)
            {
                var feature = new FeatureContext<FeatureMetadata>
                {
                    Key = (featureState.DjangoId?.ToString() ?? featureState.FeatureStateUUID),
                    Name = featureState.Feature.Name,
                    Enabled = featureState.Enabled,
                    Value = featureState.Value,
                    Priority = featureState.FeatureSegment?.Priority,
                    Metadata = new FeatureMetadata { Id = featureState.Feature.Id },
                    Variants = Array.Empty<FeatureValue>(),
                };

                if (featureState.MultivariateFeatureStateValues?.Count > 0)
                {
                    var sortedUUIDs = featureState.MultivariateFeatureStateValues
                        .Select(mv => mv.MvFsValueUUID)
                        .OrderBy(uuid => uuid)
                        .ToArray();

                    feature.Variants = featureState.MultivariateFeatureStateValues
                        .Select(mv => new FeatureValue
                        {
                            Value = mv.MultivariateFeatureOption.Value,
                            Weight = mv.PercentageAllocation,
                            Priority = mv.Id != 0 ? mv.Id : Array.IndexOf(sortedUUIDs, mv.MvFsValueUUID),
                        })
                        .ToArray();
                }

                featureContexts[feature.Name] = feature;
            }

            return featureContexts;
        }

        private static Dictionary<string, SegmentContext<SegmentMetadata, FeatureMetadata>> MapIdentityOverridesToSegments(
            List<IdentityModel> identityOverrides)
        {
            var featureIDsByName = new Dictionary<string, int>();
            var featuresToIdentifiers = new Dictionary<string, List<string>>();
            var overridesBySerializedJsonKey = new Dictionary<string, List<(string Name, bool Enabled, object Value)>>();

            foreach (var identityOverride in identityOverrides)
            {
                var identityFeatures = (identityOverride.IdentityFeatures ?? new List<FeatureStateModel>())
                    .OrderBy(f => f.Feature.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (identityFeatures.Length == 0)
                {
                    continue;
                }

                var overridesKey = new List<(string Name, bool Enabled, object Value)>();
                foreach (var featureState in identityFeatures)
                {
                    featureIDsByName[featureState.Feature.Name] = featureState.Feature.Id;
                    overridesKey.Add((
                        featureState.Feature.Name,
                        featureState.Enabled,
                        featureState.Value
                    ));
                }

                var serializedOverridesKey = JsonConvert.SerializeObject(overridesKey);

                if (!featuresToIdentifiers.ContainsKey(serializedOverridesKey))
                {
                    featuresToIdentifiers[serializedOverridesKey] = new List<string>();
                    overridesBySerializedJsonKey[serializedOverridesKey] = overridesKey;
                }
                featuresToIdentifiers[serializedOverridesKey].Add(identityOverride.Identifier);
            }

            // For each unique set of feature overrides, create a virtual segment.
            // These segments act as synthetic segments that match specific identities via an IN condition,
            // and apply the shared feature overrides to those identities with strongest priority.
            var segments = new Dictionary<string, SegmentContext<SegmentMetadata, FeatureMetadata>>();

            foreach (var kvp in featuresToIdentifiers)
            {
                var serializedOverridesKey = kvp.Key;
                var identifiers = kvp.Value;

                var segment = new SegmentContext<SegmentMetadata, FeatureMetadata>
                {
                    Key = UnusedKey,
                    Name = "identity_overrides",
                    Metadata = new SegmentMetadata
                    {
                        Source = "identity_override",
                        Id = null,
                    },
                };

                var identifiersCondition = new Condition
                {
                    Property = "$.identity.identifier",
                    Operator = Operator.In,
                    Value = new ConditionValueUnion { StringArray = identifiers.ToArray() },
                };

                var identifiersRule = new SegmentRule
                {
                    Type = TypeEnum.All,
                    Conditions = new[] { identifiersCondition },
                    Rules = Array.Empty<SegmentRule>(),
                };

                segment.Rules = new[] { identifiersRule };

                var overridesKey = overridesBySerializedJsonKey[serializedOverridesKey];

                segment.Overrides = overridesKey.Select(overrideKey => new FeatureContext<FeatureMetadata>
                {
                    Key = UnusedKey,
                    Name = overrideKey.Name,
                    Enabled = overrideKey.Enabled,
                    Value = overrideKey.Value,
                    Priority = Constants.StrongestPriority,
                    Metadata = new FeatureMetadata { Id = featureIDsByName[overrideKey.Name] },
                }).ToArray();

                var segmentKey = Utils.GetHashString(serializedOverridesKey);
                segments[segmentKey] = segment;
            }

            return segments;
        }

        private static TypeEnum MapRuleType(string type)
        {
            return type switch
            {
                Constants.AllRule => TypeEnum.All,
                Constants.AnyRule => TypeEnum.Any,
                Constants.NoneRule => TypeEnum.None,
                _ => throw new ArgumentException($"Unknown rule type: {type}"),
            };
        }

        private static Operator MapOperator(string operatorString)
        {
            return operatorString switch
            {
                Constants.Equal => Operator.Equal,
                Constants.NotEqual => Operator.NotEqual,
                Constants.GreaterThan => Operator.GreaterThan,
                Constants.GreaterThanInclusive => Operator.GreaterThanInclusive,
                Constants.LessThan => Operator.LessThan,
                Constants.LessThanInclusive => Operator.LessThanInclusive,
                Constants.Contains => Operator.Contains,
                Constants.NotContains => Operator.NotContains,
                Constants.In => Operator.In,
                Constants.Regex => Operator.Regex,
                Constants.Modulo => Operator.Modulo,
                Constants.IsSet => Operator.IsSet,
                Constants.IsNotSet => Operator.IsNotSet,
                Constants.PercentageSplit => Operator.PercentageSplit,
                _ => throw new ArgumentException($"Unknown operator: {operatorString}"),
            };
        }

        private static ConditionValueUnion MapConditionValue(string value)
        {
            return new ConditionValueUnion { String = value ?? "" };
        }
    }
}
