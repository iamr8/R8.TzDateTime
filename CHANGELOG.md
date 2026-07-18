# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.2] - 2026-07-18

### Changed
- Bump dependencies to latest per-TFM (BenchmarkDotNet, ErrorProne.NET, Meziantou.Analyzer, NodaTime, System.Text.Json, Microsoft.NET.Test.Sdk, Microsoft.SourceLink.GitHub) and pin package version ranges on the net6/net8 legs.

## [1.0.1] - 2026-07-04

### Added
- Resolve a timezone by its **Windows timezone id** (e.g. `Iran Standard Time` → `Asia/Tehran`), auto-mapped from NodaTime's CLDR data at registration. Windows ids are resolution aliases only — they are not added to `IanaIds`.

### Changed
- `LocalTimezone.Timezones` now returns a cached read-only snapshot (rebuilt only when a timezone is registered): O(1) and allocation-free per access. Previously it rebuilt a list on every read.

## [1.0.0] - 2026-07-02

### Added
- Initial release: `TimezoneDateTime`, an immutable, timezone-aware datetime built on NodaTime — a 16-byte value (UTC instant + timezone index) with allocation-free hot paths.
- `LocalTimezone` registry with a thread-safe, runtime `AddTimezone` API (UTC is the only built-in zone; register others with a `CultureInfo`, a NodaTime `CalendarSystem`, and optional aliases).
- Per-timezone calendar systems (e.g. Persian/Jalali for `Asia/Tehran`) and cultures; date components, formatting, and week boundaries resolve through the value's timezone.
- DST-correct arithmetic and day/week/month boundary helpers, `Humanize`, and ambient-timezone scopes (`LocalTimezone.Current`, `StartScope`/`EndScope`).
- `System.Text.Json` converter (UTC ISO-8601 instant wire format; timezone not serialized).
- Native-AOT- and trim-clean; targets `net6.0` and `net8.0`.

[Unreleased]: https://github.com/iamr8/R8.TzDateTime/compare/v1.0.2...HEAD
[1.0.2]: https://github.com/iamr8/R8.TzDateTime/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/iamr8/R8.TzDateTime/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/iamr8/R8.TzDateTime/releases/tag/v1.0.0
