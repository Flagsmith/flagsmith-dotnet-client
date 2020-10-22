using System.IO;

namespace BulletTrain
{
    internal static class UrlExtensions
    {
        public static string AppendPath(this string url, params string[] urlSegments)
        {
            return Path.Combine(url, Path.Combine(urlSegments));
        }
    }
}
