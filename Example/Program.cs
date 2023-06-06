using Example.Controllers;
using Example.Settings;
using Flagsmith;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddOptions<FlagsmithSettings>().Bind(builder.Configuration.GetSection(FlagsmithSettings.ConfigSection));
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<FlagsmithSettings>>().Value);

builder.Services.AddSingleton<IFlagsmithClient, FlagsmithClient>(provider =>
{
    var settings = provider.GetService<FlagsmithSettings>();
    
    return new FlagsmithClient(settings.EnvironmentKey, settings.FlagsmithApiUrl, defaultFlagHandler: DefaultFlagHandler, enableClientSideEvaluation: settings.EnableClientSideEvaluation,
        enableAnalytics: settings.EnableAnalytics, requestTimeout: settings.RequestTimeout, environmentRefreshIntervalSeconds: settings.EnvironmentRefreshIntervalSeconds);

    static Flag DefaultFlagHandler(string featureName)
    {
        if (featureName == HomeController.FeatureName)
            return new Flag(new Feature(HomeController.FeatureName), enabled: false, value: JsonConvert.SerializeObject(new { colour = "#b8b8b8" }));
        
        return new Flag() { };
    }
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
