using Flagsmith.Caching;
using Flagsmith.Caching.Impl;
using Flagsmith.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Flagsmith.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFlagsmithClient(this IServiceCollection services, Action<FlagsmithConfiguration> config)
        {
            var configuration = new FlagsmithConfiguration();
            config(configuration);

            services.AddSingleton<IFlagsmithClientConfig>(configuration);
            services.AddSingleton<ICache, MemoryCache>();

            if (configuration.EnableAnalytics)
            {
                services.AddSingleton<AnalyticsProcessor>();
                services.AddSingleton<IAnalyticsCollector>(x => x.GetRequiredService<AnalyticsProcessor>());
                services.AddSingleton<IHostedService>(x => x.GetRequiredService<AnalyticsProcessor>());
            }
            else
                services.AddSingleton<IAnalyticsCollector, FakeAnalyticsProcessor>();

            services.AddHttpClient(configuration.ApiUrl + configuration.EnvironmentKey, x =>
            {
                x.BaseAddress = new Uri(configuration.ApiUrl);
                x.Timeout = TimeSpan.FromSeconds(configuration.RequestTimeout ?? 100);
                x.DefaultRequestHeaders.Add("X-Environment-Key", configuration.EnvironmentKey);
                configuration.CustomHeaders?.ForEach(i => x.DefaultRequestHeaders.Add(i.Key, i.Value));
            });

            services.AddSingleton<IFlagsmithClient, FlagsmithClient>();
        }
    }
}
