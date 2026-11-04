using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith.Benchmarks
{
    /// <summary>
    /// Benchmarks a locally evaluating SDK client against environment documents tiered by size.
    /// </summary>
    public class SdkBenchmark
    {
        public static readonly string[] Environments = { "small", "medium", "large" };

        public static readonly (string Name, Func<SdkBenchmark, object?> Run)[] Benchmarks =
        {
            ("getEnvironmentFlags", sdkBenchmark => sdkBenchmark.GetEnvironmentFlags()),
            ("getIdentityFlagsWithIdentityOverrides", sdkBenchmark => sdkBenchmark.GetIdentityFlagsWithIdentityOverrides()),
            ("getIdentityFlagsNoTraits", sdkBenchmark => sdkBenchmark.GetIdentityFlagsNoTraits()),
            ("getIdentityFlagsWithTraits", sdkBenchmark => sdkBenchmark.GetIdentityFlagsWithTraits()),
            ("getIdentitySegmentsWithTraits", sdkBenchmark => sdkBenchmark.GetIdentitySegmentsWithTraits()),
        };

        private const string IdentityOverridesIdentifier = "identity-override-1";
        private const string Identifier = "user-1";
        private static readonly List<ITrait> Traits = new List<ITrait>
        {
            new Trait("plan", "plan-7"),
            new Trait("age", 30),
            new Trait("country", "GB"),
            new Trait("version", "2.5.1"),
            new Trait("email", "jane@example.com"),
            new Trait("user_id", 12),
            new Trait("beta", true),
            new Trait("score", 95),
            new Trait("tier", "pro"),
            new Trait("device", "iPhone 15"),
        };

        private readonly FlagsmithClient _client;

        /// <summary>
        /// Build a locally evaluating client serving the environment document from memory.
        /// </summary>
        public SdkBenchmark(string environmentsDir, string environment)
        {
            var environmentDocument = File.ReadAllText(Path.Combine(environmentsDir, $"{environment}.json"));
            _client = new FlagsmithClient(new FlagsmithConfiguration
            {
                EnvironmentKey = "ser.benchmark",
                ApiUri = new Uri("http://flagsmith.benchmark/api/v1/"),
                EnableLocalEvaluation = true,
                EnvironmentRefreshInterval = TimeSpan.FromDays(1), // Refreshed by benchmarks only
                HttpClient = new HttpClient(new EnvironmentDocumentHandler(environmentDocument)),
            });
        }

        /// <summary>
        /// Get a feature value from the environment flags.
        /// </summary>
        public string GetEnvironmentFlags()
        {
            return _client.GetEnvironmentFlags().GetAwaiter().GetResult().GetFeatureValue("feature_1").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Check a feature for an identity with identity overrides.
        /// </summary>
        public bool GetIdentityFlagsWithIdentityOverrides()
        {
            return _client.GetIdentityFlags(IdentityOverridesIdentifier).GetAwaiter().GetResult().IsFeatureEnabled("feature_8").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get a feature value for an identity without traits.
        /// </summary>
        public string GetIdentityFlagsNoTraits()
        {
            return _client.GetIdentityFlags(Identifier).GetAwaiter().GetResult().GetFeatureValue("feature_1").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get a feature value for an identity with traits.
        /// </summary>
        public string GetIdentityFlagsWithTraits()
        {
            return _client.GetIdentityFlags(Identifier, Traits).GetAwaiter().GetResult().GetFeatureValue("feature_1").GetAwaiter().GetResult();
        }

        /// <summary>
        /// List the segments of an identity with traits.
        /// </summary>
        public List<ISegment>? GetIdentitySegmentsWithTraits()
        {
            return _client.GetIdentitySegments(Identifier, Traits);
        }

        private class EnvironmentDocumentHandler : HttpMessageHandler
        {
            private readonly string _environmentDocument;

            public EnvironmentDocumentHandler(string environmentDocument)
            {
                _environmentDocument = environmentDocument;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_environmentDocument, Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
