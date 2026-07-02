# R8.TzDateTime

[![NuGet](https://img.shields.io/nuget/v/R8.TzDateTime.svg)](https://www.nuget.org/packages/R8.TzDateTime)
[![Downloads](https://img.shields.io/nuget/dt/R8.TzDateTime.svg)](https://www.nuget.org/packages/R8.TzDateTime)
[![CI](https://github.com/iamr8/R8.TzDateTime/actions/workflows/ci.yml/badge.svg)](https://github.com/iamr8/R8.TzDateTime/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An immutable, **timezone-agnostic** `DateTime` for .NET, built on [NodaTime](https://nodatime.org). A `TimezoneDateTime`
is a UTC instant tagged with a timezone: the stored value is just the instant — agnostic — and only becomes a wall-clock
date when you read it through the zone's **calendar** (Gregorian, Persian/Jalali, …) and **culture**. Values are 16
bytes, allocation-free on hot paths, and Native-AOT compatible.

Only **UTC is built in**; you register the zones your app needs (culture + calendar are your choice) —
see [Timezones are agnostic](#timezones-are-agnostic--you-register-what-you-need).

## Why

`DateTimeOffset` tells you an instant and an offset, but not *which* timezone, and it always speaks the Gregorian
calendar. `TimezoneDateTime` keeps the instant timezone-agnostic and resolves the offset, calendar, and culture from the
zone you view it through — so the date fields (`Year`, `Month`, `Day`, …) come back right without you converting
anything by hand.

## Benefits over the alternatives

|                                                    | `DateTime` | `DateTimeOffset` |    NodaTime `ZonedDateTime`     | **`TimezoneDateTime`**  |
|----------------------------------------------------|:----------:|:----------------:|:-------------------------------:|:-----------------------:|
| Knows *which* timezone (not just an offset)        |     ✗      |        ✗         |                ✓                |            ✓            |
| Non-Gregorian calendar (e.g. Persian) per zone     |     ✗      |        ✗         |             manual              | ✓ (per registered zone) |
| Culture-aware (RTL, first day of week)             |     ✗      |        ✗         |                ✗                |            ✓            |
| DST-correct arithmetic & day/week/month boundaries |     ✗      |        ✗         |                ✓                |            ✓            |
| Equal when the instant is equal, across zones      |     ✗      |        ✓         |                ✗                |            ✓            |
| Value size                                         |    8 B     |       16 B       | larger (holds a zone reference) |        **16 B**         |
| Allocation-free on hot paths                       |     ✓      |        ✓         |             mostly              |            ✓            |
| Built-in `System.Text.Json` support                |     ✓      |        ✓         |         needs a package         |            ✓            |
| Native-AOT clean                                   |     ✓      |        ✓         |                ✓                |            ✓            |

- **vs `DateTime`** — carries no timezone (only a fragile `Kind`) and is Gregorian-only. `TimezoneDateTime` binds the
  instant to a real zone and calendar, so reading `.Year`/`.Day` never silently uses the wrong timezone or calendar.
- **vs `DateTimeOffset`** — an offset is not a timezone: it can't name the zone, and math across a future DST change is
  wrong because the offset is frozen. `TimezoneDateTime` resolves offsets from the zone at the actual instant.
- **vs raw NodaTime** — NodaTime gives you the primitives; `TimezoneDateTime` bundles zone + calendar + culture into one
  16-byte value with DST-correct helpers, a built-in JSON converter, and allocation-free hot paths.

## Install

```bash
dotnet add package R8.TzDateTime
```

Targets `net6.0` and `net8.0`.

## Quick start

```csharp
using System.Globalization;
using NodaTime;
using R8.TzDateTime;

// Register the zones your app uses once at startup (only UTC is built in).
// Here Tehran is registered with the Persian calendar and the fa-IR culture:
var tehran = LocalTimezone.AddTimezone(
    "Asia/Tehran", CultureInfo.GetCultureInfo("fa-IR"), CalendarSystem.PersianSimple, "Iran");

// A TimezoneDateTime is a UTC instant tagged with a zone — agnostic until you read it.
var utc = new DateTime(2024, 3, 20, 20, 30, 0, DateTimeKind.Utc);
var value = new TimezoneDateTime(utc, tehran);

value.Year;        // 1403  (Persian, because Tehran was registered with the Persian calendar)
value.Month;       // 1     (Farvardin)
value.ToString();  // formatted with the fa-IR culture

// The same instant, viewed through another zone — equal, because equality is by instant.
var istanbul = LocalTimezone.AddTimezone("Europe/Istanbul", CultureInfo.GetCultureInfo("tr-TR"), CalendarSystem.Gregorian);
value == value.WithTimezone(istanbul); // true
```

## Core ideas

- **The value is timezone-agnostic.** Internally it is just UTC ticks plus a timezone index; the instant is absolute.
  Equality and comparison are by the instant only — two values at the same moment in different zones are equal.
- **A zone gives it a calendar and culture.** Reading `.Year`/`.Day`, formatting, and week boundaries all resolve
  through the zone you registered — so the same instant reads as `1403` in a Persian-calendar zone and `2024` in a
  Gregorian one.
- **Wall-clock constructor args are in the zone's calendar.** `new TimezoneDateTime(1403, 1, 1, tehran)` is Nowruz 1403,
  not a Gregorian date.
- **Immutable.** Every `Add*`/`GetStartOf*`/`WithTimezone` returns a new value.

## Timezones are agnostic — you register what you need

Only **UTC** is built in. The library doesn't bake in a zone list; you teach it the zones your app cares about via
`AddTimezone`, then values reference them. Registration is a **thread-safe, idempotent** runtime API — call it once at
startup (registering the same id again just returns the existing zone).

```csharp
// id + culture + calendar (+ optional aliases the zone also answers to)
LocalTimezone.AddTimezone("Asia/Tehran",  CultureInfo.GetCultureInfo("fa-IR"), CalendarSystem.PersianSimple, "Iran");
LocalTimezone.AddTimezone("Europe/London", CultureInfo.GetCultureInfo("en-GB"), CalendarSystem.Gregorian);
LocalTimezone.AddTimezone("America/New_York", CultureInfo.GetCultureInfo("en-US"), CalendarSystem.Gregorian, "US/Eastern");
```

- **culture** drives formatting, first-day-of-week, and RTL.
- **calendar** is any NodaTime `CalendarSystem` (`Gregorian`, `Iso`, `PersianSimple`, …).
- **aliases** are extra ids the zone also resolves from.

Resolve and check:

```csharp
LocalTimezone.GetTimezone("Asia/Tehran");        // or "Iran"
LocalTimezone.TryGetTimezone("Iran", out var tz);
LocalTimezone.Utc;                               // the one built-in zone
LocalTimezone.Timezones;                         // everything registered so far
```

Prefer a class? There are `AddTimezone(LocalTimezoneOptions)` and `AddTimezone<TOptions>()` overloads for subclasses of
`LocalTimezoneOptions`.

> **Why register instead of shipping a big list?** A zone's culture and calendar are *decisions*, not facts: a country
> maps to several cultures, and tzdb has no notion of which calendar you want to display. Only your app knows that
`Asia/Tehran` should render with `fa-IR` and the Persian calendar. Registering keeps that choice explicit and the
> shipped surface minimal.

## Usage

### Constructing

```csharp
new TimezoneDateTime(utcDateTime, timezone);      // from a DateTime instant
new TimezoneDateTime(ticks, timezone);            // from UTC ticks
new TimezoneDateTime(1403, 1, 1, tehran);         // wall-clock, in the zone's calendar
new TimezoneDateTime(1403, 1, 1, 9, 30, 0, tehran); // with time
TimezoneDateTime.Now;                             // now, in LocalTimezone.Current
utcDateTime.ToTimezoneDateTime(timezone);         // extension method
```

### Reading

```csharp
value.Year;  value.Month;  value.Day;
value.Hour;  value.Minute; value.Second; value.Millisecond;
value.DayOfWeek;
value.Ticks;                 // UTC ticks
value.GetUtcDateTime();      // DateTime (Utc kind)
value.GetDateTime();         // wall-clock DateTime in the zone
value.GetTimezone();         // the associated timezone
value.IsDaylightSavingTime();
```

### Arithmetic and boundaries

```csharp
value.AddYears(1); value.AddMonths(-2); value.AddDays(10);
value.AddHours(3); value.AddMinutes(15); value.AddSeconds(30);
value.Add(TimeSpan.FromHours(2)); value.Subtract(other); // TimeSpan between two values

value.GetStartOfDay();   value.GetEndOfDay();
value.GetStartOfMonth(); value.GetEndOfMonth();
value.GetStartOfWeek();  value.GetEndOfWeek();   // week honors the culture's first day
value.GetStartOfNextDay(); value.GetStartOfNextMonth(); value.GetStartOfNextWeek();
```

DST gaps and overlaps are resolved leniently (skipped local times shift forward; ambiguous ones take the earlier
offset).

### Formatting

```csharp
value.ToString();                 // general format, in the zone's culture
value.ToString("yyyy/MM/dd");     // any standard/custom date format
value.ToString(format, culture);  // override the culture
```

### Humanize (relative time)

```csharp
value.Humanize();                                    // "2 hours ago", "yesterday at 09:30", "last week"
value.Humanize(maxRelativity: TimeSpan.FromDays(7)); // fall back to a formatted date beyond the window
value.Humanize(compareAgainst: someUtcDateTime);
```

### Ambient timezone and scopes

`LocalTimezone.Current` is the default timezone for `TimezoneDateTime.Now` (it falls back to UTC when the machine's zone
hasn't been registered). In a web app, set it per request with a scope (backed by `AsyncLocal`):

```csharp
LocalTimezone.StartScope(tehran);
try
{
    var now = TimezoneDateTime.Now; // uses Tehran
}
finally
{
    LocalTimezone.EndScope();
}
```

### JSON

`TimezoneDateTime` serializes with `System.Text.Json` out of the box. The wire format is the **UTC instant** as an
ISO-8601 string — the timezone is not part of the payload, so values deserialize in UTC.

```csharp
var json = JsonSerializer.Serialize(value);          // "2024-03-20T20:30:00Z"
var back = JsonSerializer.Deserialize<TimezoneDateTime>(json);
```

## Performance

Timezone-aware operations vs the BCL `DateTime` + `TimeZoneInfo` equivalent, for a Gregorian zone (`Europe/Istanbul`) so
both sides do the same work. BenchmarkDotNet, .NET 8, Apple M-series; lower is better.

| Operation                                       | `DateTime` + `TimeZoneInfo` | `TimezoneDateTime` | Speedup |    Allocations    |
|-------------------------------------------------|----------------------------:|-------------------:|:-------:|:-----------------:|
| Convert an instant to a zone and read Y/M/D/H/M |                     46.2 ns |        **37.7 ns** |  ~1.2×  |       none        |
| Add days (DST-correct)                          |                    113.9 ns |         **6.8 ns** |  ~17×   |       none        |
| Start of day in the zone                        |                    118.7 ns |         **4.6 ns** |  ~26×   |       none        |
| Format with the zone's culture                  |                     94.8 ns |        **58.5 ns** |  ~1.6×  | 56 B (the string) |

The large gaps on add/start-of-day come from the fast paths: instants past a zone's last DST transition resolve with
plain arithmetic instead of a zone-interval lookup, and no round-trip conversion is needed. Reproduce with
`dotnet run -c Release --project benchmarks/R8.TzDateTime.Benchmarks.csproj`.

## Native AOT & trimming

The library is trim- and Native-AOT-clean and ships with the analyzers enabled (`IsAotCompatible` on net8; trim analysis
on net6). Three things to know when publishing AOT:

- **net8.0 only** — `PublishAot` is not available on net6.
- **Requires non-invariant globalization.** The point rests on cultures like `fa-IR` (Persian calendar, RTL,
  first-day-of-week). Do **not** set `<InvariantGlobalization>true</InvariantGlobalization>`, or those collapse to the
  invariant culture.
- **JSON under AOT** — the converters use no reflection, but as with any type you serialize under AOT, supply a
  source-generated `JsonSerializerContext`/resolver.

## Building & testing

```bash
dotnet build                                            # both TFMs
dotnet test tests/R8.TzDateTime.Tests.csproj -f net6.0         # net6 (xunit)
dotnet run  --project tests/R8.TzDateTime.Tests.csproj -f net8.0  # net8 (xunit.v3 self-runner)
```

Repo layout: `src/` (library), `tests/`, `benchmarks/`, `samples/` (Native-AOT smoke test).

## License

[MIT](LICENSE) — free for everyone, including commercial use.
