using Example.Settings;
using Flagsmith;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
var flagsmithClient = new FlagsmithClient(settings.EnvironmentKey, defaultFlagHandler: defaultFlagHandler);
var app = builder.Build();

static Flag defaultFlagHandler(string featureName)
{
    if (featureName == "secret_button")
        return new Flag(name:null,enabled:false,value: JsonConvert.SerializeObject("'colour': '#ababab'").ToString());
    else return new Flag() { };
}

app.Map("/", async (HttpContext req) =>
{
    if (req.Request.Query.Count > 0)
    {
        var Identifier = req.Request.Query["identifier"].ToString();
        var traitKey = req.Request.Query["trait-key"].ToString();
        var traitValue = req.Request.Query["trait-value"].ToString();
        var traitList = new List<Trait> { new Trait(traitKey, traitValue) };
        var flags = await flagsmithClient.GetIdentityFlags(Identifier, traitList);
        var showButton = await flags.IsFeatureEnabled("secret_button");
        var buttonData = flags.GetFeatureValue("secret_button").Result;


        return new
        {
            showButton = showButton,
            buttonColour = buttonData,
            identifier = Identifier
        };
    }
    else
    {

        var flag = await flagsmithClient.GetEnvironmentFlags();
        var showButton = await flag.IsFeatureEnabled("secret_button");
        var buttonData = flag.GetFeatureValue("secret_button").Result;
        return new
        {
            showButton = showButton,
            buttonColour = buttonData,
            identifier = ""
        };

    }
});

app.Run();
