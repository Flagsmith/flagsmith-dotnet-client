using Example.Extensions;
using Flagsmith;
var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterFlagsmithClientAsSingleton(builder.Configuration);
var app = builder.Build();

app.MapGet("/", async (FlagsmithClient flagsmithClient) =>
{
    var flags = await flagsmithClient.GetFeatureFlags();
    List<object> list = new List<object>();
    foreach (var f in flags)
    {

        list.Add(new
        {
            name = f.GetFeature().GetName(),
            isEnabled = f.IsEnabled(),
            value = await f.GetValue()
        });

    }
    return list;
});
app.MapPost("/", async (FlagsmithClient flagsmithClient, Example.Model.search search) =>
 {
     var traitList = new List<Trait> { new Trait(search.TraitKey, search.TraitValue) };
     var flag = await flagsmithClient.GetFeatureFlag("is_light", search.Identifier, traitList);
     return new
     {
         name = flag.GetFeature().GetName(),
         isEnabled = flag.IsEnabled(),
         value = await flag.GetValue()
     };
 });

app.Run();
