using System.Text;

namespace BulletTrain
{
    internal static class UrlExtensions
    {
        public static string AppendPath(this string url, params string[] urlSegments)
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

            return builder.ToString();
        }
    }
}
