using System;

namespace Flagsmith.Caching.Impl
{
    public interface IPooled<out T> : IDisposable where T : class
    {
        T Value { get; }
    }
}
