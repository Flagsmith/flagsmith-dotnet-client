using Flagsmith.Interfaces;
using Microsoft.Extensions.Logging;

namespace Flagsmith
{
    public class FakeAnalyticsProcessor : IAnalyticsCollector
    {
        private readonly ILogger<FakeAnalyticsProcessor> _logger;

        public FakeAnalyticsProcessor(ILogger<FakeAnalyticsProcessor> logger)
        {
            _logger = logger;
        }

        public void TrackFeature(string name)
        {
            _logger?.LogDebug("Feature '{0}' accessed, doing nothing.", name);
        }
    }
}
