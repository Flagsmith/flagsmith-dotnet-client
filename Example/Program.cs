using Example.Settings;
using Flagsmith;
using Flagsmith.Extensions;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var settings = builder.Configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
builder.Services.AddFlagsmithClient(x =>
{
    x.EnvironmentKey = settings.EnvironmentKey;
    x.DefaultFlagHandler = featureName =>
    {
        if (featureName == "secret_button")
            return new Flag(new Feature("secret_button"), enabled: false, value: JsonConvert.SerializeObject(new { colour = "#b8b8b8" }).ToString());
        else return new Flag() { };
    };
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
