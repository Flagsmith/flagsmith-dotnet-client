namespace Example.Settings
{
    public class FlagsmithSettings
    {
        public string EnvironmentKey { get; set; } = String.Empty;
        public bool UseLegacyIdentities { get; set; }
        public bool EnableClientSideEvaluation { get; set; }
        public int EnvironmentRefreshIntervalSeconds { get; set; }
        public bool EnableAnalytics { get; set; }
        public Double? RequestTimeout { get; set; }
        public int? Retries { get; set; }

    }
}
