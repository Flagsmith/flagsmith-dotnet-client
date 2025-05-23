# Changelog

# [v8.0.1](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v8.0.1) - 06 May 2025

## What's Changed
* fix: Fix default request timeout being 0 by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/155

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v8.0.0...v8.0.1

[Changes][v8.0.1]

# [v8.0.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v8.0.0) - 28 Apr 2025

## What's Changed

### BREAKING CHANGES

* fix!: Remove deprecated methods and constructors. Throw error if using local eval without server-side key by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/147

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.1.1...v8.0.0

[Changes][v8.0.0]

# [v7.1.1](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v7.1.1) - 10 Apr 2025

## What's Changed
* fix: RequestTimeout would return the seconds component of its TimeSpan instead of the total duration in seconds by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/149

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.1.0...v7.1.1

[Changes][v7.1.1]

# [v7.1.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v7.1.0) - 04 Apr 2025

## What's Changed

### Deprecated `FlagsmithClient` constructors with more than one parameter

The preferred way to instantiate a `FlagsmithClient` is now to use the `FlagsmithClient(FlagsmithConfiguration)` constructor. All other constructors are deprecated.

For example, if you were previously initialising the client like this:

```csharp
new FlagsmithClient(environmentKey: "my-key", enableAnalytics: true);
```

You should pass the equivalent `FlagsmithConfiguration` object instead:

```csharp
new FlagsmithClient(new FlagsmithConfiguration
{
    EnvironmentKey = "my-key",
    EnableAnalytics = true
});
```

This gives us more flexibility as SDK authors to add or change configuration options without requiring further breaking changes. It also allows us to provide better inline documentation for each configuration parameter.

### Deprecated `ApiUrl`, `EnableClientSideEvaluation`, and `EnvironmentRefreshIntervalSeconds` options

Instead, use `ApiUri`, `EnableLocalEvaluation`, and `EnvironmentRefreshInterval` respectively. This change makes this SDK more consistent with Flagsmith documentation and other SDKs, and improves type safety by accepting `Uri` and `TimeSpan` objects instead of deriving them from raw strings or numbers.

### Deprecated `PollingManager(Func<Task>, int)` constructor

Instead, use `new PollingManager(callback, TimeSpan.FromSeconds(intervalSeconds))`. Almost no users will be affected by this change, as `PollingManager` is meant for internal use.

[Changes][v7.1.0]

# [v7.0.1](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v7.0.1) - 15 Jan 2025

## What's Changed
* ci: fix testing workflow by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/132
* fix: missing semver dependency by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/130
* ci: fix release workflow by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/133
* chore: bump version 7.0.1 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/134

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.0.0...v7.0.1

[Changes][v7.0.1]

# [v7.0.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v7.0.0) - 18 Dec 2024

## What's Changed
* fix!: Remove flag ID properties/methods by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/129

## Breaking Changes

Version 7 removes the `Id` methods from the `IFlag` interface and `Flag` class to avoid deserialisation problems if this ID is null. A flag's internal ID is an implementation detail that should not be relevant to SDK users. If you have a use case that requires using a flag's internal ID, please create an issue here: https://github.com/Flagsmith/flagsmith-dotnet-client/issues

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v6.0.0...v7.0.0

[Changes][v7.0.0]

# [v6.0.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v6.0.0) - 18 Dec 2024

## What's Changed
* Fixes by [@mjwills-k](https://github.com/mjwills-k) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/126

## Breaking Changes
The changes in this release affect the encoding of identifiers. This is technically fixing an issue where an identifier such as `"abc&def"` would actually retrieve the flags for the identity `"abc"` and discard the `"&def"` portion. Since this change will affect the behaviour for these identities, we are marking it as a major version release. 

## New Contributors
* [@mjwills-k](https://github.com/mjwills-k) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/126

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.3...v6.0.0

[Changes][v6.0.0]

# [v5.4.3](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.4.3) - 30 Oct 2024

## What's Changed
* fix: handle null django_id in identity overrides for local evaluation mode by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/122

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.2...v5.4.3

[Changes][v5.4.3]

# [v5.4.2](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.4.2) - 17 Oct 2024

## What's Changed
* Fix constructor deadlock when using local evaluation by [@rolodato](https://github.com/rolodato) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/121

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.1...v5.4.2

[Changes][v5.4.2]

# [v5.4.1](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.4.1) - 09 Oct 2024

## What's Changed
* deps: bump artifact options by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/115
* Add ConfigureAwait(false) to Flags by [@ben-buckli](https://github.com/ben-buckli) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/118

## New Contributors
* [@ben-buckli](https://github.com/ben-buckli) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/118

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.0...v5.4.1

[Changes][v5.4.1]

# [v5.4.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.4.0) - 11 Sep 2024

## What's Changed
* feat: Transient identities and traits by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/111
* Add ConfigureAwait(false) to AnalyticsProcessor by [@DavidPerish](https://github.com/DavidPerish) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/117

## New Contributors
* [@DavidPerish](https://github.com/DavidPerish) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/117

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.2...v5.4.0

[Changes][v5.4.0]

# [v5.3.2](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.3.2) - 29 Jul 2024

## What's Changed
* fix: OfflineHandler causes the SDK to bypass live flag lookups by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/110

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.1...v5.3.2

[Changes][v5.3.2]

# [v5.3.1](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.3.1) - 18 Jul 2024

## What's Changed
* fix: Prevent SynchronizationContext deadlock on dotnet framework by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/107

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.0...v5.3.1

[Changes][v5.3.1]

# [v5.3.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.3.0) - 19 Apr 2024

## What's Changed
* feat: Offline mode by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/86
* feat: Identity overrides in local evaluation mode by [@khvn26](https://github.com/khvn26) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/90
* feat: Implement multi-threading support for analytics Processor by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/95
* chore(infra): Publish NuGet package automatically by [@khvn26](https://github.com/khvn26) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/97
* chore(infra): test against dotnet core 8 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/91
* chore: Delete example by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/92

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.2...v5.3.0

[Changes][v5.3.0]

# [Version 5.2.2 (v5.2.2)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.2.2) - 19 Apr 2024

## What's Changed
* fix: Ensure environment is retrieved on start polling by [@novakzaballa](https://github.com/novakzaballa) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/83
* fix: exception on get identity cache by [@vpetrusevici](https://github.com/vpetrusevici) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/84

## New Contributors
* [@vpetrusevici](https://github.com/vpetrusevici) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/84

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.1...v5.2.2

[Changes][v5.2.2]

# [Version 5.2.1 (v5.2.1)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.2.1) - 27 Oct 2023

## What's Changed
* Fix cache initialisation that was some kind of circular reference. by [@JFCote](https://github.com/JFCote) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/81

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.0...v5.2.1

[Changes][v5.2.1]

# [Version 5.2.0 (v5.2.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.2.0) - 26 Oct 2023

## What's Changed
* chore: test support for other .NET versions by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/75
* Add caching by [@JFCote](https://github.com/JFCote) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/77

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.1.0...v5.2.0

[Changes][v5.2.0]

# [Version 5.1.0 (v5.1.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.1.0) - 26 Oct 2023

## What's Changed
* feat: implement `IN` operator by [@khvn26](https://github.com/khvn26) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/72

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.0.1...v5.1.0

[Changes][v5.1.0]

# [Version 5.0.1 (v5.0.1)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.0.1) - 22 Jun 2023

## What's Changed
* feat: accept FlagsmithConfiguration in FlagsmithClient & small refactor by [@luk355](https://github.com/luk355) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/67

## New Contributors
* [@luk355](https://github.com/luk355) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/67

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.0.0...v5.0.1

[Changes][v5.0.1]

# [v5.0.0](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v5.0.0) - 15 Jun 2023

## What's Changed
* *BREAKING CHANGE*: fix: consistent split evaluations by [@khvn26](https://github.com/khvn26) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/69

**WARNING**: We modified the local evaluation behaviour. You may see different flags returned to identities attributed to your percentage split-based segments after upgrading to this version.

## New Contributors
* [@khvn26](https://github.com/khvn26) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/69

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.3...v5.0.0

[Changes][v5.0.0]

# [v4.3.3](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.3.3) - 15 Jun 2023

## What's Changed
* Bump copyright notices by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/63
* Add Environment API key to identity init by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/65

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.2...v4.3.3

[Changes][v4.3.3]

# [v4.3.2](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.3.2) - 08 Jun 2023

## What's Changed
* Feature/use interfaces by [@tberger](https://github.com/tberger) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/61
* Bump version 4.3.2 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/62

## New Contributors
* [@tberger](https://github.com/tberger) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/61

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.1...v4.3.2

[Changes][v4.3.2]

# [Version 4.3.1 (v4.3.1)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.3.1) - 29 Mar 2023

## What's Changed
* Release 4.3.1 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/60

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.0...v4.3.1

[Changes][v4.3.1]

# [Version 4.3.0 (v4.3.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.3.0) - 07 Mar 2023

## What's Changed
* Update engine test data submodule by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/58
* Add interfaces to allow unit testing in .NET app using the FlagsmithClient by [@JFCote](https://github.com/JFCote) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/57
* Release 4.3.0 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/59

## New Contributors
* [@JFCote](https://github.com/JFCote) made their first contribution in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/57

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.2.1...v4.3.0

[Changes][v4.3.0]

# [Version 4.2.1 (v4.2.1)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.2.1) - 03 Nov 2022

## What's Changed
* Use identity get request if no traits by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/54
* Release 4.2.1 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/53

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.2.0...v4.2.1

[Changes][v4.2.1]

# [Version 4.2.0 (v4.2.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.2.0) - 01 Nov 2022

## What's Changed
* Add Modulo operator by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/50
* Add IS_SET and IS_NOT_SET operators by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/51
* Release 4.2.0 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/49

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.1.0...v4.2.0

[Changes][v4.2.0]

# [Version 4.1.0 (v4.1.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.1.0) - 29 Jul 2022

## What's Changed
* Add GetIdentitySegments method by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/45
* Release 4.1.0 by [@matthewelwell](https://github.com/matthewelwell) in https://github.com/Flagsmith/flagsmith-dotnet-client/pull/46

**Full Changelog**: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.0.3...v4.1.0

[Changes][v4.1.0]

# [Version 4.0.0 (v4.0.0)](https://github.com/Flagsmith/flagsmith-dotnet-client/releases/tag/v4.0.0) - 07 Jun 2022


[Changes][v4.0.0]

[v8.0.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v8.0.0...v8.0.1
[v8.0.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.1.1...v8.0.0
[v7.1.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.1.0...v7.1.1
[v7.1.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.0.1...v7.1.0
[v7.0.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v7.0.0...v7.0.1
[v7.0.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v6.0.0...v7.0.0
[v6.0.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.3...v6.0.0
[v5.4.3]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.2...v5.4.3
[v5.4.2]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.1...v5.4.2
[v5.4.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.4.0...v5.4.1
[v5.4.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.2...v5.4.0
[v5.3.2]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.1...v5.3.2
[v5.3.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.3.0...v5.3.1
[v5.3.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.2...v5.3.0
[v5.2.2]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.1...v5.2.2
[v5.2.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.2.0...v5.2.1
[v5.2.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.1.0...v5.2.0
[v5.1.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.0.1...v5.1.0
[v5.0.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v5.0.0...v5.0.1
[v5.0.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.3...v5.0.0
[v4.3.3]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.2...v4.3.3
[v4.3.2]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.1...v4.3.2
[v4.3.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.3.0...v4.3.1
[v4.3.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.2.1...v4.3.0
[v4.2.1]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.2.0...v4.2.1
[v4.2.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.1.0...v4.2.0
[v4.1.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/compare/v4.0.0...v4.1.0
[v4.0.0]: https://github.com/Flagsmith/flagsmith-dotnet-client/tree/v4.0.0

<!-- Generated by https://github.com/rhysd/changelog-from-release v3.7.2 -->
