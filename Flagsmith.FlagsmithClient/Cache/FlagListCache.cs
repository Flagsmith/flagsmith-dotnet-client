using System;
using System.Threading.Tasks;
using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    public abstract class FlagListCache
    {
        private readonly int _cacheDurationInMinutes;
        
        public delegate Task<Flags> GetRegularFlagsDelegate();
        public delegate Task<Flags> GetIdentityFlagsDelegate(IdentityTraitsKey identityTraitsKey);

        protected Flags _flags;
        protected readonly IDateTimeProvider _dateTimeProvider;
        protected DateTime _timestamp;

        protected FlagListCache(IDateTimeProvider dateTimeProvider, Flags flags, int cacheDurationInMinutes)
        {
            _flags = flags;
            _dateTimeProvider = dateTimeProvider;
            _timestamp = dateTimeProvider.Now();
            _cacheDurationInMinutes = cacheDurationInMinutes;
        }

        protected bool IsCacheStale()
        {
            return _dateTimeProvider.Now().Subtract(_timestamp).TotalMinutes > _cacheDurationInMinutes;
        }
    }
}