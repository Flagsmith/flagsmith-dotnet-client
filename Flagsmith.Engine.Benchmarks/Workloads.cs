using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FlagsmithEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flagsmith.Engine.Benchmarks
{
    /// <summary>
    /// Evaluation contexts benchmarked against the engine. Built only from the public context model, so the same
    /// workloads run against engine revisions that predate Flag Dependency.
    /// </summary>
    public static class Workloads
    {
        private const int FeatureCount = 200;
        private const int MultivariateFeatureCount = 20;
        private const int TraitSegmentCount = 50;
        private const int DependencyCount = 10;
        private const string FlagDependencyCasePrefix = "test_flag_dependency__";

        private static readonly string[] Plans = { "free", "pro", "premium" };
        private static readonly string[] Countries = { "gb", "us", "de", "fr", "br", "jp" };

        /// <summary>
        /// An environment of 200 features (20 multivariate) and 50 segments with nested rules on identity traits.
        /// </summary>
        public static EvaluationContext<object, object> TraitSegments(bool withIdentity)
        {
            var context = NewContext(withIdentity);
            for (var index = 0; index < TraitSegmentCount; index++)
            {
                AddSegment(context, $"trait_segment_{index}", TraitRules(index), Override($"feature_{index * 2 % FeatureCount}", index), Override($"feature_{(index * 2 + 1) % FeatureCount}", index));
            }
            return context;
        }

        /// <summary>
        /// <see cref="TraitSegments"/> plus 10 segments with conditions on JSONPath properties of the context.
        /// </summary>
        public static EvaluationContext<object, object> JsonPathSegments()
        {
            var context = TraitSegments(withIdentity: true);
            for (var index = 0; index < DependencyCount; index++)
            {
                var rules = new[]
                {
                    Rule(TypeEnum.All, Condition("$.identity.identifier", Operator.Equal, "identity_" + index), Condition("$.environment.name", Operator.Equal, "Benchmarks")),
                };
                AddSegment(context, $"jsonpath_segment_{index}", rules, Override($"feature_{index}", index));
            }
            return context;
        }

        /// <summary>
        /// <see cref="TraitSegments"/> plus 10 prerequisite features enabled by trait segments, and 10 segments that
        /// override a dependent feature when its prerequisite flag is enabled (`$.flags.prerequisite_N.enabled`).
        /// </summary>
        public static EvaluationContext<object, object> FlagDependencySegments() => DependentSegments(readFlags: true);

        /// <summary>
        /// Same as <see cref="FlagDependencySegments"/>, except the dependent segments repeat the prerequisite
        /// segments' trait rules instead of reading flags: how such targeting is configured without Flag Dependency.
        /// </summary>
        public static EvaluationContext<object, object> RepeatedTraitSegments() => DependentSegments(readFlags: false);

        /// <summary>
        /// engine-test-data cases, either the flag_dependency ones or all others.
        /// </summary>
        public static EvaluationContext<object, object>[] EngineTestDataCases(bool flagDependency, [CallerFilePath] string sourcePath = "")
        {
            var testCasesPath = Path.Combine(Path.GetDirectoryName(sourcePath), "..", "Flagsmith.EngineTest", "EngineTestData", "test_cases");
            return Directory.GetFiles(testCasesPath, "*.json*")
                .Where(path => Path.GetFileName(path).StartsWith(FlagDependencyCasePrefix) == flagDependency)
                .OrderBy(path => path)
                .Select(path => JObject.Parse(File.ReadAllText(path), new JsonLoadSettings { CommentHandling = CommentHandling.Ignore })["context"].ToObject<EvaluationContext<object, object>>())
                .ToArray();
        }

        private static EvaluationContext<object, object> DependentSegments(bool readFlags)
        {
            var context = TraitSegments(withIdentity: true);
            for (var index = 0; index < DependencyCount; index++)
            {
                var prerequisiteName = $"prerequisite_{index}";
                AddFeature(context, prerequisiteName, enabled: false);
                AddFeature(context, $"dependent_{index}", enabled: false);

                var prerequisiteRules = new[]
                {
                    Rule(TypeEnum.All, Condition("plan", Operator.Equal, "premium"), Condition("country", Operator.In, string.Join(",", Countries.Take(index % Countries.Length + 1)))),
                };
                AddSegment(context, $"prerequisite_segment_{index}", prerequisiteRules, Override(prerequisiteName, null));

                var dependentRules = readFlags
                    ? new[] { Rule(TypeEnum.All, Condition($"$.flags.{prerequisiteName}.enabled", Operator.Equal, "true")) }
                    : prerequisiteRules;
                AddSegment(context, $"dependent_segment_{index}", dependentRules, Override($"dependent_{index}", null));
            }
            return context;
        }

        private static EvaluationContext<object, object> NewContext(bool withIdentity)
        {
            var context = new EvaluationContext<object, object>
            {
                Environment = new EnvironmentContext { Key = "benchmarks-environment-key", Name = "Benchmarks" },
                Features = new Dictionary<string, FeatureContext<object>>(),
                Segments = new Dictionary<string, SegmentContext<object, object>>(),
            };
            for (var index = 0; index < FeatureCount; index++)
            {
                AddFeature(context, $"feature_{index}", enabled: index % 2 == 0);
                if (index < MultivariateFeatureCount)
                {
                    context.Features[$"feature_{index}"].Variants = new[]
                    {
                        new FeatureValue { Value = "variant_a", Weight = 30, Priority = 0 },
                        new FeatureValue { Value = "variant_b", Weight = 30, Priority = 1 },
                        new FeatureValue { Value = "variant_c", Weight = 40, Priority = 2 },
                    };
                }
            }
            if (withIdentity)
            {
                context.Identity = new IdentityContext
                {
                    Identifier = "identity_3",
                    Traits = new Dictionary<string, object>
                    {
                        ["plan"] = "premium",
                        ["country"] = "gb",
                        ["age"] = 34,
                        ["email"] = "someone@example.com",
                        ["signup_year"] = 2021,
                        ["beta_tester"] = true,
                        ["language"] = "en",
                        ["device"] = "ios",
                        ["app_version"] = "4.2.0",
                        ["score"] = 7.5,
                    },
                };
            }
            return context;
        }

        private static SegmentRule[] TraitRules(int index)
        {
            var conditions = new List<Condition> { Condition("plan", Operator.Equal, Plans[index % Plans.Length]) };
            if (index % 5 == 0)
                conditions.Add(Condition("age", Operator.GreaterThan, (18 + index).ToString()));
            if (index % 10 == 0)
                conditions.Add(Condition("email", Operator.Regex, @".*@example\.com$"));
            var anyRule = Rule(
                TypeEnum.Any,
                Condition("country", Operator.In, string.Join(",", Countries.Skip(index % Countries.Length).Take(2))),
                Condition("", Operator.PercentageSplit, "50"));
            var rule = Rule(TypeEnum.All, conditions.ToArray());
            rule.Rules = new[] { anyRule };
            return new[] { rule };
        }

        private static void AddFeature(EvaluationContext<object, object> context, string name, bool enabled)
        {
            context.Features[name] = new FeatureContext<object> { Key = name + "_key", Name = name, Enabled = enabled, Value = name + "_value" };
        }

        private static void AddSegment(EvaluationContext<object, object> context, string name, SegmentRule[] rules, params FeatureContext<object>[] overrides)
        {
            context.Segments[name] = new SegmentContext<object, object> { Key = name, Name = name, Rules = rules, Overrides = overrides };
        }

        private static FeatureContext<object> Override(string featureName, double? priority)
        {
            return new FeatureContext<object> { Key = featureName + "_key", Name = featureName, Enabled = true, Value = featureName + "_overridden", Priority = priority };
        }

        private static SegmentRule Rule(TypeEnum type, params Condition[] conditions) => new SegmentRule { Type = type, Conditions = conditions };

        private static Condition Condition(string property, Operator @operator, string value) => new Condition { Property = property, Operator = @operator, Value = value };
    }
}
