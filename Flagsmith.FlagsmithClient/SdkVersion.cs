#nullable enable

namespace Flagsmith
{
    /// <summary>
    /// Provides SDK version information for User-Agent header
    /// </summary>
    public static class SdkVersion
    {
        // x-release-please-start-version
        internal const string Version = "10.1.0";
        // x-release-please-end

        /// <summary>
        /// Gets the SDK version in the format "flagsmith-dotnet-sdk/version"
        /// </summary>
        public static string GetUserAgent()
        {
            return $"flagsmith-dotnet-sdk/{Version}";
        }
    }
}
