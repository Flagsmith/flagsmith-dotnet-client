using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using OfflineHandler;
using System.Net.Http;

namespace Flagsmith
{
    public class FlagsmithConfiguration
    {
        private static readonly Uri DefaultApiUri = new Uri("https://edge.api.flagsmith.com/api/v1/");
        private TimeSpan _timeout;

        /// <summary>
        /// Versioned base Flagsmith API URI to use for all requests. Defaults to
        /// <c>https://edge.api.flagsmith.com/api/v1/</c>.
        /// <example><code>new Uri("https://flagsmith.example.com/api/v1/")</code></example>
        /// </summary>
        public Uri ApiUri { get; set; } = DefaultApiUri;

        /// <summary>
        /// The environment key obtained from Flagsmith interface.
        /// </summary>
        public string EnvironmentKey { get; set; }

        /// <summary>
        /// Enables local evaluation of flags.
        /// </summary>
        public bool EnableLocalEvaluation { get; set; }

        /// <summary>
        /// If using local evaluation, specify the interval period between refreshes of local environment data.
        /// </summary>
        public TimeSpan EnvironmentRefreshInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Callable which will be used in the case where flags cannot be retrieved from the API or a non existent feature is requested.
        /// </summary>
        public Func<string, Flag>? DefaultFlagHandler { get; set; }

        /// <summary>
        /// Provide logger for logging polling info & errors which is only applicable when client side evalution is enabled and analytics errors.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// if enabled, sends additional requests to the Flagsmith API to power flag analytics charts.
        /// </summary>
        public bool EnableAnalytics { get; set; }

        /// <summary>
        /// Number of seconds to wait for a request to complete before terminating the request
        /// </summary>
        public Double? RequestTimeout
        {
            get => _timeout.Seconds;
            set => _timeout = TimeSpan.FromSeconds(value ?? 100);
        }

        /// <summary>
        /// Total http retries for every failing request before throwing the final error.
        /// </summary>
        public int? Retries { get; set; }

        /// <summary>
        /// Additional headers to add to requests made to the Flagsmith API
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; }

        /// <summary>
        /// If enabled, the SDK will cache the flags for the duration specified in the CacheConfig
        /// </summary>
        public CacheConfig CacheConfig { get; set; } = new CacheConfig(false);

        /// <summary>
        /// Indicates whether the client is in offline mode.
        /// </summary>
        public bool OfflineMode { get; set; }

        /// <summary>
        /// Handler for offline mode operations.
        /// </summary>
        public BaseOfflineHandler? OfflineHandler { get; set; }

        /// <summary>
        /// Http client used for flagsmith-API requests.
        /// </summary>
        public HttpClient HttpClient { get; set; } = new HttpClient();
    }
}
