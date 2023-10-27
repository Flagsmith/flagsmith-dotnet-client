using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class RegularFlagListCache : FlagListCache
    {
        public RegularFlagListCache(IDateTimeProvider dateTimeProvider,
            int cacheDurationInMinutes) :
            base(dateTimeProvider, cacheDurationInMinutes)
        {
        }

        public IFlags GetLatestFlags(GetRegularFlagsDelegate getFlagsDelegate)
        {
            if (IsCacheStale())
            {
                _flags = getFlagsDelegate().Result;
                _timestamp = _dateTimeProvider.Now();
            }

            return _flags;
        }
    }
}