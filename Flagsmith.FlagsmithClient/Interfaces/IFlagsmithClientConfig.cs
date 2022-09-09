using System;
using System.Collections.Generic;

namespace Flagsmith.Interfaces
{
    public interface IFlagsmithClientConfig
    {
        /// <summary>
        /// Override the URL of the Flagsmith API to communicate with.
        /// </summary>
        string ApiUrl { get; }

        /// <summary>
        /// The environment key obtained from Flagsmith interface.
        /// </summary>
        string EnvironmentKey { get; }

        /// <summary>
        /// If using local evaluation, specify the interval period between refreshes of local environment data.
        /// </summary>
        int EnvironmentRefreshIntervalSeconds { get; }

        /// <summary>
        /// Enables local evaluation of flags.
        /// </summary>
        bool EnableClientSideEvaluation { get; }

        /// <summary>
        /// Callable which will be used in the case where flags cannot be retrieved from the API or a non existent feature is requested.
        /// </summary>
        Func<string, IFlag> DefaultFlagHandler { get; }

        /// <summary>
        /// if enabled, sends additional requests to the Flagsmith API to power flag analytics charts.
        /// </summary>
        bool EnableAnalytics { get; }

        /// <summary>
        /// Number of seconds to wait for a request to complete before terminating the request
        /// </summary>
        double? RequestTimeout { get; }

        /// <summary>
        /// Total http retries for every failing request before throwing the final error.
        /// </summary>
        int? Retries { get; }

        /// <summary>
        /// Additional headers to add to requests made to the Flagsmith API
        /// </summary>
        IReadOnlyDictionary<string, string> CustomHeaders { get; }
    }
}
