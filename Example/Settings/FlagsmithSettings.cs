using Example.Controllers;
using Flagsmith;
using Newtonsoft.Json;

namespace Example.Settings
{
    public class FlagsmithSettings : IFlagsmithConfiguration
    {
        public static string ConfigSection => "FlagsmithConfiguration";

        public string ApiUrl { get; set; } = "https://edge.api.flagsmith.com/api/v1/";
        public string EnvironmentKey { get; set; } = String.Empty;
        public bool EnableClientSideEvaluation { get; set; } = false;
        public int EnvironmentRefreshIntervalSeconds { get; set; } = 60;
        public ILogger Logger { get; set; }
        public bool EnableAnalytics { get; set; } = false;
        public Double? RequestTimeout { get; set; }
        public Dictionary<string, string> CustomHeaders { get; set; }
        public int? Retries { get; set; } = 1;
        public CacheConfig CacheConfig { get; set; } = new(false);

        public Func<string, Flag> DefaultFlagHandler { get; set; } = featureName =>
        {
            if (featureName == HomeController.FeatureName)
            {
                return new Flag(new Feature(HomeController.FeatureName), enabled: false, value: JsonConvert.SerializeObject(new { colour = "#b8b8b8" }));
            }

            return new Flag();
        };

        public bool IsValid() => true;
    }
}