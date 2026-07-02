using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NodaTime;
using R8.TzDateTime;

BenchmarkRunner.Run<TzBenchmarks>();

// Compares TimezoneDateTime against the BCL DateTime + TimeZoneInfo equivalent for timezone-aware work.
// A Gregorian zone (Europe/Istanbul) is used so both sides can do the operation apples-to-apples;
// the Persian-calendar capability is unique to TimezoneDateTime and has no BCL equivalent to compare.
[MemoryDiagnoser]
[MediumRunJob]
public class TzBenchmarks
{
    private static readonly DateTime Utc = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
    private static readonly LocalTimezone Tz = LocalTimezone.AddTimezone("Europe/Istanbul", CultureInfo.GetCultureInfo("tr-TR"), CalendarSystem.Gregorian);
    private static readonly TimeZoneInfo Tzi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private static readonly TimezoneDateTime Value = new(Utc, Tz);

    [Benchmark]
    public int Tz_ConvertAndReadParts()
    {
        var v = new TimezoneDateTime(Utc, Tz);
        return v.Year + v.Month + v.Day + v.Hour + v.Minute;
    }

    [Benchmark]
    public int Bcl_ConvertAndReadParts()
    {
        var v = TimeZoneInfo.ConvertTimeFromUtc(Utc, Tzi);
        return v.Year + v.Month + v.Day + v.Hour + v.Minute;
    }

    [Benchmark]
    public long Tz_AddDaysInZone() => Value.AddDays(3).Ticks;

    [Benchmark]
    public long Bcl_AddDaysInZone()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(Utc, Tzi).AddDays(3);
        return TimeZoneInfo.ConvertTimeToUtc(local, Tzi).Ticks;
    }

    [Benchmark]
    public long Tz_StartOfDayInZone() => Value.GetStartOfDay().Ticks;

    [Benchmark]
    public long Bcl_StartOfDayInZone()
    {
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(Utc, Tzi).Date;
        return TimeZoneInfo.ConvertTimeToUtc(startLocal, Tzi).Ticks;
    }

    [Benchmark]
    public int Tz_FormatInZone() => Value.ToString("yyyy-MM-dd HH:mm").Length;

    [Benchmark]
    public int Bcl_FormatInZone() => TimeZoneInfo.ConvertTimeFromUtc(Utc, Tzi).ToString("yyyy-MM-dd HH:mm").Length;
}
