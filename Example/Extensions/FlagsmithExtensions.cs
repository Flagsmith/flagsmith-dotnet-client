using Example.Settings;
using Flagsmith;
namespace Example.Extensions
{
    public static class FlagsmithExtensions
    {
        public static void RegisterFlagsmithClientAsSingleton(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
            var flagsmithClient = new FlagsmithClient(MapFlagsmithConfiguration(settings));
            services.AddSingleton<FlagsmithClient>(flagsmithClient);
        }
        private static FlagsmithConfiguration MapFlagsmithConfiguration(FlagsmithSettings settings)
        => new FlagsmithConfiguration
        {
            EnvironmentKey = settings.EnvironmentKey,
            EnableClientSideEvaluation = settings.EnableClientSideEvaluation,
            UseLegacyIdentities = settings.UseLegacyIdentities,
            RequestTimeout = settings.RequestTimeout,
            Retries = settings.Retries,
            EnableAnalytics = settings.EnableAnalytics,
            EnvironmentRefreshIntervalSeconds = settings.EnvironmentRefreshIntervalSeconds,
        };
    }
}
