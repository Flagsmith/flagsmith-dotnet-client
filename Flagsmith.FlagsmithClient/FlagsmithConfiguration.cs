using Flagsmith.Interfaces;
using System;
using System.Collections.Generic;

namespace Flagsmith
{
    public class FlagsmithConfiguration : IFlagsmithClientConfig
    {
        public const string DefaultApiUrl = "https://edge.api.flagsmith.com/api/v1/";

        public FlagsmithConfiguration()
        {
            ApiUrl = DefaultApiUrl;
            EnvironmentKey = string.Empty;
            EnvironmentRefreshIntervalSeconds = 60;
        }

        /// <summary>
        /// Override the URL of the Flagsmith API to communicate with.
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// The environment key obtained from Flagsmith interface.
        /// </summary>
        public string EnvironmentKey { get; set; }

        /// <summary>
        /// Enables local evaluation of flags.
        /// </summary>
        public bool EnableClientSideEvaluation { get; set; }

        /// <summary>
        /// If using local evaluation, specify the interval period between refreshes of local environment data.
        /// </summary>
        public int EnvironmentRefreshIntervalSeconds { get; set; }

        /// <summary>
        /// Callable which will be used in the case where flags cannot be retrieved from the API or a non existent feature is requested.
        /// </summary>
        public Func<string, IFlag> DefaultFlagHandler { get; set; }

        /// <summary>
        /// if enabled, sends additional requests to the Flagsmith API to power flag analytics charts.
        /// </summary>
        public bool EnableAnalytics { get; set; }

        /// <summary>
        /// Number of seconds to wait for a request to complete before terminating the request
        /// </summary>
        public double? RequestTimeout { get; set; }

        /// <summary>
        /// Total http retries for every failing request before throwing the final error.
        /// </summary>
        public int? Retries { get; set; }

        /// <summary>
        /// Additional headers to add to requests made to the Flagsmith API
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; }

        IReadOnlyDictionary<string, string> IFlagsmithClientConfig.CustomHeaders => CustomHeaders;

        public static FlagsmithConfiguration From(
            string environmentKey,
            string apiUrl = DefaultApiUrl,
            Func<string, IFlag> defaultFlagHandler = null,
            bool enableAnalytics = false,
            bool enableClientSideEvaluation = false,
            int environmentRefreshIntervalSeconds = 60,
            Dictionary<string, string> customHeaders = null,
            int? retries = null,
            double? requestTimeout = null)
        {
            return new FlagsmithConfiguration
            {
                EnvironmentKey = environmentKey,
                ApiUrl = apiUrl,
                DefaultFlagHandler = defaultFlagHandler,
                EnableAnalytics = enableAnalytics,
                EnableClientSideEvaluation = enableClientSideEvaluation,
                EnvironmentRefreshIntervalSeconds = environmentRefreshIntervalSeconds,
                CustomHeaders = customHeaders,
                Retries = retries,
                RequestTimeout = requestTimeout,
            };
        }
    }
}
