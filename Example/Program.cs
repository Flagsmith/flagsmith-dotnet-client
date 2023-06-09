using Example.Settings;
using Flagsmith;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddOptions<FlagsmithSettings>().Bind(builder.Configuration.GetSection(FlagsmithSettings.ConfigSection));
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<FlagsmithSettings>>().Value);

builder.Services.AddSingleton<IFlagsmithClient, FlagsmithClient>(provider =>
{
    var settings = provider.GetService<FlagsmithSettings>();
    
    return new FlagsmithClient(settings);
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
