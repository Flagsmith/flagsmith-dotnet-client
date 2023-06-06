namespace Example.Settings
{
    public class FlagsmithSettings
    {
        public static string ConfigSection => "FlagsmithConfiguration";
        
        public string FlagsmithApiUrl { get; set; } = "https://edge.api.flagsmith.com/api/v1/";
        public string EnvironmentKey { get; set; } = String.Empty;
        public bool EnableClientSideEvaluation { get; set; } = false;
        public int EnvironmentRefreshIntervalSeconds { get; set; } = 60;
        public bool EnableAnalytics { get; set; } = false;
        public Double? RequestTimeout { get; set; }
        public int Retries { get; set; } = 1;

    }
}
