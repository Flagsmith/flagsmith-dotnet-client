using System;

namespace Flagsmith.Caching
{
    public interface ICacheItemPolicy
    {
        DateTimeOffset AbsoluteExpiration { get; set; }
        TimeSpan SlidingExpiration { get; set; }
    }
}
