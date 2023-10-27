using System;
using System.Collections.Generic;
using Flagsmith.Providers;

namespace Flagsmith.Cache
{
    internal class IdentityFlagListCache : FlagListCache
    {
        private readonly IdentityWrapper _identityWrapper;

        public IdentityFlagListCache(IdentityWrapper identityWrapper,
            IDateTimeProvider dateTimeProvider,
            AnalyticsProcessor analyticsProcessor,
            Func<string, IFlag> defaultFlagHandler,
            int cacheDurationInMinutes) :
            base(dateTimeProvider,
                new Flags(new List<IFlag>(), analyticsProcessor, defaultFlagHandler),
                cacheDurationInMinutes)
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