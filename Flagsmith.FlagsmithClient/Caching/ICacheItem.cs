using System;

namespace Flagsmith.Caching
{
    public interface ICacheItem
    {
        string Key { get; }
        Guid UniqueKey { get; }
        Type ValueType { get; }
        DateTimeOffset Created { get; }
        DateTimeOffset LastAccessed { get; }
        DateTimeOffset AbsoluteExpiration { get; }
        TimeSpan SlidingExpiration { get; }
    }
}
