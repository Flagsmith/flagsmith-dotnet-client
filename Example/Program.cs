using Example.Settings;
using Flagsmith;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
var flagsmithClient = new FlagsmithClient(settings.EnvironmentKey, defaultFlagHandler: defaultFlagHandler);
var app = builder.Build();

static Flag defaultFlagHandler(string featureName)
{
    if (featureName == "is_light")
        return new Flag() { Value = JsonConvert.SerializeObject("'colour': '#b8b8b8'").ToString(), Enabled = false };
    else return new Flag() { };
}

app.MapGet("/", async (HttpContext req) =>
{
    if (req.Request.Query.Count > 0)
    {
        var Identifier = req.Request.Query["identifier"].ToString();
        var traitKey = req.Request.Query["trait-key"].ToString();
        var traitValue = req.Request.Query["trait-value"].ToString();
        var traitList = new List<Trait> { new Trait(traitKey, traitValue) };
        var flags = await flagsmithClient.GetFeatureFlags(Identifier, traitList);
        var showButton = await flags.IsFeatureEnabled("is_light");
        var buttonData = flags.GetFeatureValue("is_light").Result;


        return new
        {
            showButton = showButton,
            buttonColour = buttonData,
            identifier = Identifier
        };
    }
    else
    {

        var flag = await flagsmithClient.GetFeatureFlags();
        var showButton = await flag.IsFeatureEnabled("is_light");
        var buttonData = flag.GetFeatureValue("is_light").Result;
        return new
        {
            showButton = showButton,
            buttonColour = buttonData,
            identifier = ""
        };

    }
});

app.Run();
