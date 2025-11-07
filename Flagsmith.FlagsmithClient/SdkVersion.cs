#nullable enable

using System.Reflection;

namespace Flagsmith
{
    /// <summary>
    /// Provides SDK version information for User-Agent header
    /// </summary>
    public static class SdkVersion
    {
        private static string? _version;

        /// <summary>
        /// Gets the SDK version in the format "flagsmith-dotnet-sdk/version"
        /// </summary>
        public static string GetUserAgent()
        {
            if (_version == null)
            {
                var assembly = typeof(SdkVersion).Assembly;
                var version = assembly.GetName().Version;
                
                if (version != null)
                {
                    // Use Major.Minor.Build if Build >= 0, otherwise use Major.Minor
                    if (version.Build >= 0)
                    {
                        _version = $"flagsmith-dotnet-sdk/{version.Major}.{version.Minor}.{version.Build}";
                    }
                    else
                    {
                        _version = $"flagsmith-dotnet-sdk/{version.Major}.{version.Minor}";
                    }
                }
                else
                {
                    _version = "flagsmith-dotnet-sdk/unknown";
                }
            }

            return _version;
        }
    }
}
