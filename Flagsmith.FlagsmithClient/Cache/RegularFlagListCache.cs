using System;
using System.Collections.Generic;
using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class RegularFlagListCache : FlagListCache
    {
        public RegularFlagListCache(IDateTimeProvider dateTimeProvider,
            AnalyticsProcessor analyticsProcessor,
            Func<string, IFlag> defaultFlagHandler,
            int cacheDurationInMinutes) :
            base(dateTimeProvider,
                new Flags(new List<IFlag>(), analyticsProcessor, defaultFlagHandler),
                cacheDurationInMinutes)
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