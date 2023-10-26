using System;
using System.Threading.Tasks;
using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    public abstract class FlagListCache
    {
        private readonly int _cacheDurationInMinutes;
        public delegate Task<IFlags> GetRegularFlagsDelegate();
        public delegate Task<IFlags> GetIdentityFlagsDelegate(IdentityWrapper identityWrapper);

        protected IFlags _flags;
        protected readonly IDateTimeProvider _dateTimeProvider;
        protected DateTime _timestamp;

        protected FlagListCache(IDateTimeProvider dateTimeProvider, IFlags flags, int cacheDurationInMinutes)
        {
            _flags = flags;
            _dateTimeProvider = dateTimeProvider;

            //This is to ensure that the cache is stale on first run
            _timestamp = dateTimeProvider.Now().Subtract(new TimeSpan(0, 0, cacheDurationInMinutes + 1, 0));

            _cacheDurationInMinutes = cacheDurationInMinutes;
        }

        protected bool IsCacheStale()
        {
            return _dateTimeProvider.Now().Subtract(_timestamp).TotalMinutes > _cacheDurationInMinutes;
        }
    }
}