using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class IdentityFlagListCache : FlagListCache
    {
        private readonly IdentityWrapper _identityWrapper;

        public IdentityFlagListCache(IdentityWrapper identityWrapper, IFlags flags, IDateTimeProvider dateTimeProvider, int cacheDurationInMinutes) :
            base(dateTimeProvider, flags, cacheDurationInMinutes)
        {
            _identityWrapper = identityWrapper;
        }

        public IFlags GetLatestFlags(GetIdentityFlagsDelegate getFlagsDelegate)
        {
            if (IsCacheStale())
            {
                _flags = getFlagsDelegate(_identityWrapper).Result;
                _timestamp = _dateTimeProvider.Now();
            }

            return _flags;
        }
    }
}