using System.Text;

namespace Flagsmith
{
    internal static class UrlExtensions
    {
        public static string AppendPath(this string url, params string[] urlSegments)
        {
            return url.AppendToUrl(true, urlSegments);
        }

        public static string AppendToUrl(this string url, bool trailingSlash, params string[] urlSegments)
        {
            var builder = new StringBuilder(url);
            if (!url.EndsWith("/"))
            {
                builder.Append('/');
            }

            foreach (var segment in urlSegments)
            {
                builder.Append(segment);
                if (!segment.EndsWith("/"))
                {
                    builder.Append('/');
                }
            }

            return trailingSlash ? builder.ToString() : builder.ToString(0, builder.Length - 1);
        }
    }
}
