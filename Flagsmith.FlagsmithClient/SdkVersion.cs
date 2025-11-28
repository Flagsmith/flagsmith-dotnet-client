#nullable enable

namespace Flagsmith
{
    /// <summary>
    /// Provides SDK version information for User-Agent header
    /// </summary>
    public static class SdkVersion
    {
        // x-release-please-start-version
        private const string Version = "9.0.0";
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
