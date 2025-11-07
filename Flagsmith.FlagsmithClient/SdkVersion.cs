#nullable enable

using System;
using System.Reflection;

namespace Flagsmith
{
    /// <summary>
    /// Provides SDK version information for User-Agent header
    /// </summary>
    public static class SdkVersion
    {
        private static readonly Lazy<string> _version = new Lazy<string>(() =>
        {
            var assembly = typeof(SdkVersion).Assembly;
            var version = assembly.GetName().Version;
            
            if (version != null)
            {
                // Use Major.Minor.Build if Build > -1, otherwise use Major.Minor
                if (version.Build > -1)
                {
                    return $"flagsmith-dotnet-sdk/{version.Major}.{version.Minor}.{version.Build}";
                }
                else
                {
                    return $"flagsmith-dotnet-sdk/{version.Major}.{version.Minor}";
                }
            }
            else
            {
                return "flagsmith-dotnet-sdk/unknown";
            }
        });

        /// <summary>
        /// Gets the SDK version in the format "flagsmith-dotnet-sdk/version"
        /// </summary>
        public static string GetUserAgent()
        {
            return _version.Value;
        }
    }
}
