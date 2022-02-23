using Example.Extensions;
using Flagsmith;
var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterFlagsmithClientAsSingleton(builder.Configuration);
var app = builder.Build();


app.MapPost("/", async (FlagsmithClient flagsmithClient, Example.Model.Search search) =>
{
    var traitList = new List<Trait> { new Trait(search.TraitKey, search.TraitValue) };
    var flags = await flagsmithClient.GetFeatureFlags(search.Identifier, traitList);
    var flag = await flags.GetFeatureFlag("is_light");
    return new
    {
        name = flag.GetFeature().GetName(),
        isEnabled = flag.IsEnabled(),
        value = flag.GetValue()
    };
});

app.Run();
