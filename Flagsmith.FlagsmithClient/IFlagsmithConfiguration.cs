using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Flagsmith
{
    public interface IFlagsmithConfiguration
    {
        /// <summary>
        /// Override the URL of the Flagsmith API to communicate with.
        /// </summary>
        string ApiUrl { get; set; }

        /// <summary>
        /// The environment key obtained from Flagsmith interface.
        /// </summary>
        string EnvironmentKey { get; set; }

        /// <summary>
        /// Enables local evaluation of flags.
        /// </summary>
        bool EnableClientSideEvaluation { get; set; }

        /// <summary>
        /// If using local evaluation, specify the interval period between refreshes of local environment data.
        /// </summary>
        int EnvironmentRefreshIntervalSeconds { get; set; }

        /// <summary>
        /// Callable which will be used in the case where flags cannot be retrieved from the API or a non existent feature is requested.
        /// </summary>
        Func<string, Flag> DefaultFlagHandler { get; set; }

        /// <summary>
        /// Provide logger for logging polling info & errors which is only applicable when client side evalution is enabled and analytics errors.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// if enabled, sends additional requests to the Flagsmith API to power flag analytics charts.
        /// </summary>
        bool EnableAnalytics { get; set; }

        /// <summary>
        /// Number of seconds to wait for a request to complete before terminating the request
        /// </summary>
        Double? RequestTimeout { get; set; }

        /// <summary>
        /// Total http retries for every failing request before throwing the final error.
        /// </summary>
        int? Retries { get; set; }

        /// <summary>
        /// Additional headers to add to requests made to the Flagsmith API
        /// </summary>
        Dictionary<string, string> CustomHeaders { get; set; }

        bool IsValid();
    }
}