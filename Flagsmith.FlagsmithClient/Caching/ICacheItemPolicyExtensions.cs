using Flagsmith.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flagsmith.Caching
{
    public static class ICacheItemPolicyExtensions
    {
        public static bool IsStillValid(this ICacheItemPolicy cip, DateTimeOffset lastAccessed)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if (now >= cip.AbsoluteExpiration)
                return false;

            if (cip.SlidingExpiration != TimeSpan.MaxValue && now >= lastAccessed + cip.SlidingExpiration)
                return false;

            return true;
        }

        public static void Remove<T>(this ICache cache, string key, IEnumerable<T> values)
        {
            if (values == null || !values.Any())
                return;

            values.ForEach(x => cache.Remove(string.Format(key, x)));
        }
    }
}
