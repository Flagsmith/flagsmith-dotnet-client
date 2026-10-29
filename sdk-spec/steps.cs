// Steps for the Flagsmith SDK behavioural spec: https://github.com/Flagsmith/sdk-spec
//
// Each method implements the Gherkin step in the comment above it.

using System.Globalization;
using Flagsmith;

public static class Steps
{
    // Create the SDK client
    public static FlagsmithClient CreateClient(string environmentKey, string apiUrl) =>
        new(new FlagsmithConfiguration { EnvironmentKey = environmentKey, ApiUri = new Uri(apiUrl) });

    // Create the SDK client with events enabled
    public static FlagsmithClient CreateClientWithEvents(string environmentKey, string apiUrl, string eventsApiUrl) =>
        new(new FlagsmithConfiguration
        {
            EnvironmentKey = environmentKey,
            ApiUri = new Uri(apiUrl),
            EnableEvents = true,
            EventsApiUri = new Uri(eventsApiUrl),
        });

    // When the application gets the environment flags
    public static async Task<IEnumerable<object>> GetEnvironmentFlags(FlagsmithClient flagsmith) =>
        AsSpecFlags(await flagsmith.GetEnvironmentFlags());

    // When the application gets the flags for identity "{identifier}" with traits:
    public static async Task<IEnumerable<object>> GetIdentityFlags(
        FlagsmithClient flagsmith, string identifier, Dictionary<string, object?> traits) =>
        AsSpecFlags(await flagsmith.GetIdentityFlags(identifier, AsTraits(traits)));

    // When the application gets the experiment flag "{feature}" for identity "{identifier}" with traits:
    public static async Task<IEnumerable<object>> GetExperimentFlag(
        FlagsmithClient flagsmith, string feature, string identifier, Dictionary<string, object?> traits) =>
        new[] { AsSpecFlag(await flagsmith.GetExperimentFlag(feature, identifier, AsTraits(traits))) };

    // When the application tracks event "{event}" for identity "{identifier}" with value {value} and metadata:
    public static void TrackEvent(
        FlagsmithClient flagsmith, string eventName, string identifier, object? value, Dictionary<string, object?> metadata) =>
        flagsmith.TrackEvent(eventName, identifier, value, metadata: metadata);

    // When the application tracks an exposure of feature "{feature}" for identity "{identifier}" with value {value} and metadata:
    public static void TrackExposureEvent(
        FlagsmithClient flagsmith, string feature, string identifier, object? value, Dictionary<string, object?> metadata) =>
        flagsmith.TrackExposureEvent(feature, identifier, value, metadata: metadata);

    private static List<ITrait> AsTraits(Dictionary<string, object?> traits) =>
        traits.Select(trait => (ITrait)new Trait(trait.Key, trait.Value)).ToList();

    private static IEnumerable<object> AsSpecFlags(IFlags flags) => flags.AllFlags().Select(AsSpecFlag);

    private static object AsSpecFlag(IFlag flag) =>
        new { feature = flag.GetFeatureName(), enabled = flag.Enabled, value = AsSpecValue(flag.Value) };

    // The SDK exposes flag values as strings (42 -> "42", true -> "true"), so restore numbers and booleans.
    private static object? AsSpecValue(string? value) =>
        bool.TryParse(value, out var boolean) ? boolean
        : decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number
        : value;
}
