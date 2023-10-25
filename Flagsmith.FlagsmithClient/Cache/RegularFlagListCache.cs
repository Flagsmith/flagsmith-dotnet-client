using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class RegularFlagListCache : FlagListCache
    {
        public RegularFlagListCache(IDateTimeProvider dateTimeProvider, IFlags flags, int cacheDurationInMinutes) :
            base(dateTimeProvider, flags, cacheDurationInMinutes)
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