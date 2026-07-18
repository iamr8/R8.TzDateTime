## Workflow (required)

Always follow TDD. For any bug or feature:
1. Write a failing test that captures the issue/requirement first.
2. Run it and confirm it fails for the expected reason (reproduce).
3. Implement the fix/feature.
4. Re-run the tests and confirm they pass (both TFMs — see Commands).

Do not write implementation before a failing test exists.

Always verify changes by building the solution and running the tests before claiming done. The build must be completely clean — treat any warning or error (compiler or analyzer, on either TFM) as a failure to fix, not to ignore.

Any concern raised by **GitHub Advanced Security** (CodeQL / code scanning, the `github-advanced-security` bot on a PR) must be **strictly resolved, never dismissed or skipped** — treat these findings like build errors that block the merge, and fix the underlying code.

Changes must be verified against both net6.0 and net8.0. When net8.0 offers improved functionality (APIs, performance) over net6.0, use it conditionally via `#if NET8_0_OR_GREATER` rather than settling for the lowest common denominator on both.

This is a performance-sensitive library. Every addition, refactor, or change must be benchmarked and compared against the baseline before/after. A change must not regress performance, and must not make output flaky or noisy, nor introduce heap allocations on hot paths (the allocation and stress test suites — `TimezoneDateTimeAllocationTests`, `MemoryTests`, `TimezoneStressTests`, `TimezoneConcurrencyTests` — guard this; keep them green).

## Project

R8.TzDateTime is a .NET library (net6.0 + net8.0) providing `TimezoneDateTime` — an immutable, timezone-aware datetime struct built on NodaTime, with per-timezone calendar systems (e.g. Persian calendar for Asia/Tehran) and cultures.

## Commands

```bash
dotnet build                                                       # build solution (both TFMs)
dotnet test tests/R8.TzDateTime.Tests.csproj -f net6.0             # net6 tests (xunit 2.x / VSTest)
dotnet run  --project tests/R8.TzDateTime.Tests.csproj -f net8.0   # net8 tests (xunit.v3 self-runner)
dotnet run  --project tests/R8.TzDateTime.Tests.csproj -f net8.0 -- -trx out.trx           # net8 with a TRX report
dotnet test tests/R8.TzDateTime.Tests.csproj -f net6.0 --filter "FullyQualifiedName~TimezoneTests"   # single class (net6)
```

Notes:
- The two TFMs use different test stacks — net8.0 uses xunit.v3 (a self-running executable; run it with `dotnet run`, not `dotnet test`), net6.0 uses xunit 2.x. See `Usings.cs`. Changes must compile and pass on both.
- Run tests with the pinned 6.0.x/8.0.x SDK (as CI does). A newer default SDK (e.g. 10) routes `dotnet test` through the new test platform and breaks the net6 VSTest invocation — that's an SDK-default artifact, not a test failure.

## Versioning & release (CI)

SemVer, driven solely by `<Version>` in `src/R8.TzDateTime.csproj`. To cut a release:

1. **Bump `<Version>`** in `src/R8.TzDateTime.csproj` (e.g. `1.0.0` → `1.0.1`) — do this first.
2. **Commit and push to `develop`.**
3. Open a PR `develop` → `main`. `main` is protected (no direct pushes); the required checks **Build & test** and **Native AOT smoke test** must pass before it can merge.
4. **Merging to `main`** runs the Release workflow (`release.yml`). Its `check` job publishes **only if `<Version>` is strictly newer** than the latest on NuGet; otherwise it logs a warning and skips (so workflow-only merges to `main` never publish). On a newer version it tags `vX.Y.Z`, creates the GitHub release (`github-release` environment), and pushes to NuGet via Trusted Publishing/OIDC (`nuget` environment) — see `NuGet/login@v1` + the `NUGET_USER` secret.

Never publish by hand and never push straight to `main`. The nuget.org Trusted Publishing policy must name **Workflow File `release.yml`** and **Environment `nuget`**.

## Architecture

Repo layout: each category folder holds its project file directly — `src/R8.TzDateTime.csproj` (library), `tests/R8.TzDateTime.Tests.csproj` (xunit, `InternalsVisibleTo`), `benchmarks/R8.TzDateTime.Benchmarks.csproj` (BenchmarkDotNet), `samples/AotSmoke.csproj` (Native-AOT smoke test). `R8.TzDateTime.sln` includes all four; CI builds the **test project** (not the solution) so the net8-only benchmark/sample can't break the net6 leg. The `.sln` is classic format (not `.slnx`) so the .NET 8 SDK in CI can read it — don't let the IDE convert it.

### Core value type: `TimezoneDateTime` (TimezoneDateTime.cs)

A `readonly struct` holding only **UTC ticks (`long`) + a timezone index (`ushort`)**. Every constructor funnels into the private `(long ticks, ushort timezoneIndex)` one. Date components (Year/Month/Day, constructor args) are expressed in the **calendar of the value's timezone** (e.g. Persian if that zone was registered with the Persian calendar). Equality/comparison operate on the instant only; two values at the same instant in different timezones are equal.

### Timezone registry: `LocalTimezone` (LocalTimezone.cs)

Flyweight registry backed by static `ConcurrentDictionary`s keyed by IANA id, plus a **volatile** `_byIndex` array for lock-free index → timezone resolution on hot paths. **UTC is the only built-in timezone** (its own type `UtcTimezone`, registered first at index 0 — `default(TimezoneDateTime)`, JSON null, and the UTC fast path all depend on that). Every other zone is registered at runtime via the public **`AddTimezone`** (id + `CultureInfo` + `CalendarSystem` + aliases, an options overload, and a generic subclass overload). Culture/calendar are supplied by the caller — they can't be reliably derived (culture-per-country is ambiguous; tzdb lacks aliases like "Iraq").

`AddTimezone` mutates the registry at runtime, so it is thread-safe by design: writers serialize on `_registrationLock`; the publication order inside the lock is **grow + `Volatile.Write(_byIndex)` first, then `_options.TryAdd` last** (the id-resolvable write is the visibility gate) so a value built from a just-added id can never resolve to an index the lookup table hasn't grown to yet. Reads stay lock-free via the volatile `_byIndex`. If you touch registration, keep `TimezoneConcurrencyTests` green — it guards this. `LocalTimezone.Current` falls back to UTC when the system zone isn't registered (never throws).

Each zone is **also resolvable by its Windows timezone id** (e.g. `"Iran Standard Time"` → `Asia/Tehran`): at registration, `GetWindowsAliases` looks up the zone's Windows id via NodaTime's CLDR `TzdbToWindowsIds` and registers it as an extra `_options`/`_instances` key. So resolution stays a single exact-match lookup (no fallback/hot-path cost); Windows ids are resolution keys only and are **not** added to the public `IanaIds`.

`LocalTimezone.Timezones` returns a **cached read-only snapshot** (`_timezonesView`, a `ReadOnlyCollection` over `_byIndex`) rebuilt only at registration — O(1), zero-allocation per access. Do not revert it to a per-access LINQ projection (`_options.Values.Select(...).Distinct().ToArray()`); that allocates on every read (guarded by `TimezoneDateTimeAllocationTests.Timezones_property_access_should_not_allocate`).

Data-driven tests that need "the registered zones" enumerate the fixed `TestTimezones.Ids`, **not** the live `LocalTimezone.Timezones` — the registry is process-global and mutable, so enumerating it makes case counts non-deterministic (and risks concurrent-modification) once other tests register zones.

`LocalTimezone.Current` is the ambient timezone: an `AsyncLocal` scope (`StartScope`/`EndScope`, intended for ASP.NET Core per-request use) layered over a process-wide default resolved from the system timezone (UTC fallback).

Tests register the zones they need via a `[ModuleInitializer]` in `tests/TestTimezones.cs` (Tehran/Baghdad/Istanbul), since they are no longer built in.

### Performance fast paths (the non-obvious part)

The library deliberately reimplements slices of NodaTime to avoid allocations on hot paths:

- **Final-interval cache**: at registration, `CacheFinalIntervalUnsafe` probes each zone's last tzdb interval (the one with no end). Instants past the last transition resolve with constant-offset arithmetic instead of zone-interval lookups. `long.MaxValue` sentinel disables the fast path.
- **`ResolveLenientTicksCore` / `ResolveStartOfDayTicksCore`**: allocation-free re-implementations of NodaTime's lenient resolver and `AtStartOfDayInZone`. These are `internal` and **pinned against NodaTime by `TimezoneResolverEquivalenceTests`** — any change to their semantics must keep those tests passing.
- **UTC fast path**: `_timezoneIndex == UtcIndex` skips zone conversion entirely; Gregorian/ISO calendars (`UsesGregorianCalendar`) use BCL `DateTime` math instead of NodaTime calendar arithmetic.

The test suite enforces this design: `TimezoneDateTimeAllocationTests` and `MemoryTests` check allocation behavior, `TimezoneCalendarParityTests` and `TimezoneResolverEquivalenceTests` check parity with NodaTime, plus concurrency and stress tests. When touching hot-path code, run these suites, not just the functional tests.

### JSON serialization

`System.Text.Json` converter (factory at the bottom of TimezoneDateTime.cs). The wire format is an ISO 8601 UTC instant string only — **the timezone is not serialized**; values always deserialize with the UTC timezone. JSON null reads as `TimezoneDateTime.Empty` for the non-nullable type. The converters use no reflection, so they are Native-AOT safe (consumers must still supply a source-gen `JsonSerializerContext`/resolver, as with any type under AOT).

### Native AOT / trimming

The library is AOT- and trim-clean and must stay that way. net8.0 sets `IsAotCompatible` (full AOT+trim+single-file analyzers); net6.0 sets `IsTrimmable`+`EnableTrimAnalyzer`. Keep both TFMs free of `ILxxxx` warnings — e.g. use generic `Enum.GetValues<T>()`, avoid reflection-based serialization in library code. AOT publishing itself is net8-only (net6 can't `PublishAot`). The library **requires non-invariant globalization** — under `InvariantGlobalization=true`, `fa-IR`/Persian calendar/RTL collapse to invariant; verify with a native binary, not just a clean build.

## Conventions

- Roslyn analyzers are enforced on the library project (ErrorProne.NET, Roslynator, Meziantou) — build warnings from them matter.
- NodaTime and System.Text.Json package versions are pinned per-TFM in the csproj (conditional `ItemGroup`s).
- Tests use FluentAssertions 8.
