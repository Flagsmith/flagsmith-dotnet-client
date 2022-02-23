using Example.Settings;
using Flagsmith;
namespace Example.Extensions
{
    public static class FlagsmithExtensions
    {
        public static void RegisterFlagsmithClientAsSingleton(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
            var flagsmithClient = new FlagsmithClient(settings.EnvironmentKey);
            services.AddSingleton<FlagsmithClient>(flagsmithClient);
        }
    }
}
