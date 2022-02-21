using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
namespace Flagsmith
{
    public class FlagsmithConfiguration
    {
        public FlagsmithConfiguration()
        {
            ApiUrl = "https://api.flagsmith.com/api/v1/";
            EnvironmentKey = string.Empty;
            UseLegacyIdentities = true;
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
        public bool UseLegacyIdentities { get; set; }
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
        public Func<string, Flag> DefaultFlagHandler { get; set; }
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
        public Double? RequestTimeout { get; set; }
        /// <summary>
        /// Total http retries for every failing request before throwing the final error.
        /// </summary>
        public int? Retries { get; set; }
        /// <summary>
        /// Additional headers to add to requests made to the Flagsmith API
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; }
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(EnvironmentKey);
        }
    }
}
