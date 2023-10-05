using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class IdentityFlagListCache : FlagListCache
    {
        private readonly IdentityTraitsKey _identityTraitsKey;

        public IdentityFlagListCache(IdentityTraitsKey identityTraitsKey, IFlags flags, IDateTimeProvider dateTimeProvider, int cacheDurationInMinutes) : 
            base(dateTimeProvider, flags, cacheDurationInMinutes)
        {
            _identityTraitsKey = identityTraitsKey;
        }

        public IFlags GetLatestFlags(GetIdentityFlagsDelegate getFlagsDelegate)
        {
            if (IsCacheStale())
            {
                _flags = getFlagsDelegate(_identityTraitsKey).Result;
                _timestamp = _dateTimeProvider.Now();
            }

            return _flags;
        }
    }
}