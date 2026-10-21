// Adapter that lets the Flagsmith SDK behavioural spec drive this SDK.
//
// Protocol: https://github.com/Flagsmith/sdk-spec/blob/main/ADAPTER.md

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flagsmith;

var clients = new ConcurrentDictionary<string, FlagsmithClient>();
var nextClientId = 0;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");
var app = builder.Build();

// Report SDK errors to the runner.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception e)
    {
        app.Logger.LogError(e, "Request failed");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = $"{e.GetType().Name}: {e.Message}" });
    }
});

app.MapGet("/health", () => new
{
    sdk = "flagsmith-dotnet-client",
    // The SDK only exposes its version through the User-Agent, e.g. "flagsmith-dotnet-sdk/9.0.0".
    version = SdkVersion.GetUserAgent().Split('/').Last(),
});

app.MapPost("/clients", (CreateClientRequest request) =>
{
    var clientId = Interlocked.Increment(ref nextClientId).ToString(CultureInfo.InvariantCulture);
    clients[clientId] = new FlagsmithClient(new FlagsmithConfiguration
    {
        EnvironmentKey = request.EnvironmentKey,
        ApiUri = new Uri(request.ApiUrl),
    });
    return Results.Json(new { clientId }, statusCode: 201);
});

app.MapPost("/clients/{clientId}/environment-flags", async (string clientId) =>
    clients.TryGetValue(clientId, out var client)
        ? Results.Json(SerialiseFlags(await client.GetEnvironmentFlags()))
        : ClientNotFound(clientId));

app.MapPost("/clients/{clientId}/identity-flags", async (string clientId, IdentityFlagsRequest request) =>
{
    if (!clients.TryGetValue(clientId, out var client))
    {
        return ClientNotFound(clientId);
    }
    var traits = (request.Traits ?? new())
        .Select(trait => (ITrait)new Trait(trait.Key, FromJson(trait.Value)))
        .ToList();
    return Results.Json(SerialiseFlags(await client.GetIdentityFlags(request.Identifier, traits)));
});

// The SDK has nothing to dispose of: it is not IDisposable and stops polling in its finaliser.
app.MapDelete("/clients/{clientId}", (string clientId) =>
    clients.TryRemove(clientId, out _) ? Results.NoContent() : ClientNotFound(clientId));

app.Run();

static IResult ClientNotFound(string clientId) =>
    Results.Json(new { error = $"No client {clientId}" }, statusCode: 404);

static object SerialiseFlags(IFlags flags) => new
{
    flags = flags.AllFlags().Select(flag => new
    {
        featureName = flag.GetFeatureName(),
        enabled = flag.Enabled,
        value = FlagValue.FromSdk(flag.Value),
    }),
};

// Traits are passed to the SDK as CLR primitives so that it sends them with their JSON type.
static object? FromJson(JsonElement value) => value.ValueKind switch
{
    JsonValueKind.String => value.GetString(),
    JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
    JsonValueKind.Number => value.GetDouble(),
    JsonValueKind.True => true,
    JsonValueKind.False => false,
    JsonValueKind.Null => null,
    _ => throw new ArgumentException($"Unsupported trait value: {value}"),
};

record CreateClientRequest(string EnvironmentKey, string ApiUrl);

record IdentityFlagsRequest(string Identifier, Dictionary<string, JsonElement>? Traits);

static partial class FlagValue
{
    // The SDK exposes every flag value as a string: Flagsmith's JSON value is read into `IFlag.Value`
    // as its JSON text (42 -> "42", true -> "true"). Restore the JSON type of numbers and booleans.
    // This is lossy: a string value "42" or "true" is indistinguishable from a number or boolean.
    public static object? FromSdk(string? value) =>
        value is not null && JsonPrimitive().IsMatch(value)
            ? JsonSerializer.Deserialize<JsonElement>(value)
            : value;

    [GeneratedRegex(@"^(true|false|-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?)$")]
    private static partial Regex JsonPrimitive();
}
