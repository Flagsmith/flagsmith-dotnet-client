using Flagsmith.Interfaces;
using Microsoft.Extensions.Logging;

namespace Flagsmith
{
    public class NullAnalyticsProcessor : IAnalyticsCollector
    {
        private readonly ILogger<NullAnalyticsProcessor> _logger;

        public NullAnalyticsProcessor(ILogger<NullAnalyticsProcessor> logger)
        {
            _logger = logger;
        }

        public void TrackFeature(string name)
        {
            _logger?.LogDebug("Feature '{0}' accessed, doing nothing.", name);
        }
    }
}
