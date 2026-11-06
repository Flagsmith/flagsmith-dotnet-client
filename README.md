# Flagsmith .NET SDK

The SDK for .NET Core, .NET Framework, Mono, Xamarin and Universal Windows Platform applications for [https://www.flagsmith.com/](https://www.flagsmith.com/).

# Flagsmith .NET Engine

This Project powers the core Flagsmith API flag evaluations engine.

# Flagsmith .NET Engine-Test

This Project contains all the Test Cases to evaluate the Engine functionality.

## Adding to your project

For full documentation visit [https://docs.flagsmith.com/clients/server-side](https://docs.flagsmith.com/clients/server-side).

## Caching

Flags can be cached either in process, via `CacheConfig`, or in a
[`HybridCache`](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) owned by your
application, via `HybridCacheConfig`. The two are mutually exclusive. `HybridCacheConfig` lets flags
be shared across instances through whatever distributed (L2) cache you have configured, and only
requires the `Microsoft.Extensions.Caching.Hybrid` package on your side — the SDK itself depends on
the abstraction alone.

```csharp
builder.Services.AddHybridCache();

var flagsmith = new FlagsmithClient(new FlagsmithConfiguration
{
    EnvironmentKey = "<your key>",
    HybridCacheConfig = new HybridCacheConfig(serviceProvider.GetRequiredService<HybridCache>())
    {
        Duration = TimeSpan.FromMinutes(5),
    },
});
```

### Keeping identity flags in step with traits

Flagsmith evaluates an identity against the traits it holds for it, and it only learns of a trait
when the SDK sends it. A cached flag list would therefore hide trait changes until it expired.

`RefreshOnTraitChanges` opts in to preventing that: a call to `GetIdentityFlags` that carries a trait
Flagsmith has not been told about, or a new value for one it has, discards the cached entry and
fetches again, so the change reaches Flagsmith immediately. Traits that are merely absent from a call
do not trigger a refresh — Flagsmith keeps the traits it already holds, so omitting one cannot change
the flags it returns, and neither can sending one that is unchanged.

```csharp
HybridCacheConfig = new HybridCacheConfig(hybridCache)
{
    RefreshOnTraitChanges = true,
}
```

It is off by default, which keeps evaluation stateless and consistent with the other server-side
SDKs: cached flags are then only ever refreshed once `Duration` has elapsed, and a trait change waits
that duration out.

### Serialization

Cache entries are written as JSON. On .NET 9 and later this goes through a source-generated
`System.Text.Json` context, so no runtime reflection is involved and the path stays trimming and AOT
friendly; `netstandard2.0` falls back to `Newtonsoft.Json`. Both emit the same JSON, so instances on
different target frameworks can share one distributed cache.

Nothing needs registering on your side either way: entries are stored as strings, so your
`HybridCache` does not need a serializer for SDK types.

## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/kyle-ssg/c36a03aebe492e45cbd3eefb21cb0486) for details on our code of conduct, and the process for submitting pull requests

## Getting Help

If you encounter a bug or feature request we would like to hear about it. Before you submit an issue please search existing issues in order to prevent duplicates.

## Get in touch

If you have any questions about our projects you can email <a href="mailto:support@flagsmith.com">support@flagsmith.com</a>.

## Useful links

[Website](https://www.flagsmith.com/)
