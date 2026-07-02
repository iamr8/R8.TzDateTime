namespace R8.TzDateTime.Tests;

/// <summary>
///     Reproductions for defects found in the TimezoneDateTime investigation.
///     Each test documents the expected (fixed) behavior; they fail against the pre-fix implementation.
/// </summary>
public class TimezoneDateTimeBugReproTests
{
    private static LocalTimezone Tehran => LocalTimezone.GetTimezone("Asia/Tehran");
    private static LocalTimezone Istanbul => LocalTimezone.GetTimezone("Europe/Istanbul");

    // ---------------------------------------------------------------
    // A1 — AddDays/AddMonths must not throw on DST transitions.
    // Iran observed DST until Sep 2022 with midnight transitions, so
    // historical Tehran data hits skipped/ambiguous local times.
    // ---------------------------------------------------------------

    [Fact]
    public void AddDays_should_not_throw_when_target_local_time_falls_into_dst_gap()
    {
        // Persian 1401-01-01 00:30 = 2022-03-21 00:30 +03:30 (UTC 2022-03-20T21:00Z).
        // Next day 00:30 falls into the skipped hour (2022-03-22 00:00->01:00).
        var tzdt = new TimezoneDateTime(1401, 1, 1, 0, 30, 0, Tehran);
        tzdt.GetUtcDateTime().Should().Be(new DateTime(2022, 3, 20, 21, 0, 0, DateTimeKind.Utc));

        var act = () => tzdt.AddDays(1);

        // Lenient resolution shifts the skipped time forward by the gap: 01:30 +04:30 = 21:00Z.
        act.Should().NotThrow();
        act().GetUtcDateTime().Should().Be(new DateTime(2022, 3, 21, 21, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AddDays_should_not_throw_when_target_local_time_is_ambiguous()
    {
        // Persian 1401-06-29 23:30 = 2022-09-20 23:30 +04:30 (UTC 19:00Z).
        // Next day 23:30 is ambiguous (fall-back repeats 23:00-24:00 on 2022-09-21).
        var tzdt = new TimezoneDateTime(1401, 6, 29, 23, 30, 0, Tehran);
        tzdt.GetUtcDateTime().Should().Be(new DateTime(2022, 9, 20, 19, 0, 0, DateTimeKind.Utc));

        var act = () => tzdt.AddDays(1);

        // Lenient resolution picks the earlier offset (+04:30): 2022-09-21T19:00Z.
        act.Should().NotThrow();
        act().GetUtcDateTime().Should().Be(new DateTime(2022, 9, 21, 19, 0, 0, DateTimeKind.Utc));
    }

    // ---------------------------------------------------------------
    // A2 — AddYears must clamp the day like DateTime.AddYears instead
    // of throwing for leap days (Feb 29 / Esfand 30).
    // ---------------------------------------------------------------

    [Fact]
    public void AddYears_should_clamp_gregorian_leap_day_instead_of_throwing()
    {
        var tzdt = new TimezoneDateTime(2024, 2, 29, 12, 0, 0, Istanbul);

        var act = () => tzdt.AddYears(1);

        act.Should().NotThrow();
        var result = act();
        (result.Year, result.Month, result.Day).Should().Be((2025, 2, 28));
    }

    [Fact]
    public void AddYears_should_clamp_persian_leap_day_instead_of_throwing()
    {
        // 1399 is a Persian leap year: Esfand 30 exists (= 2021-03-20). 1400 is not.
        var tzdt = new TimezoneDateTime(1399, 12, 30, 12, 0, 0, Tehran);

        var act = () => tzdt.AddYears(1);

        act.Should().NotThrow();
        var result = act();
        (result.Year, result.Month, result.Day).Should().Be((1400, 12, 29));
    }

    // ---------------------------------------------------------------
    // A3 — GetStartOfNextWeek must agree with GetStartOfWeek: exactly
    // one week later. The UTC path used to land on Monday (+8 days from
    // a Sunday) while GetStartOfWeek uses Sunday-first weeks.
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(2024, 1, 7)] // Sunday
    [InlineData(2024, 1, 8)] // Monday
    [InlineData(2024, 1, 13)] // Saturday
    public void GetStartOfNextWeek_should_be_exactly_one_week_after_GetStartOfWeek_for_utc(int year, int month, int day)
    {
        var tzdt = new TimezoneDateTime(new DateTime(year, month, day, 15, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        var startOfWeek = tzdt.GetStartOfWeek();
        var startOfNextWeek = tzdt.GetStartOfNextWeek();

        startOfNextWeek.Should().Be(startOfWeek.AddDays(7));
    }

    [Fact]
    public void GetStartOfNextWeek_should_be_exactly_one_week_after_GetStartOfWeek_for_tehran()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 10, 20, 30, Tehran);

        tzdt.GetStartOfNextWeek().Should().Be(tzdt.GetStartOfWeek().AddDays(7));
    }

    // ---------------------------------------------------------------
    // A8 — End-of-X must be the last representable tick of the range,
    // i.e. start-of-next-X minus exactly one tick. The UTC paths used
    // to stop at .999 ms, leaving a 9999-tick hole.
    // ---------------------------------------------------------------

    [Fact]
    public void GetEndOfDay_should_be_one_tick_before_start_of_next_day_for_utc()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc).AddTicks(1234567), LocalTimezone.Utc);

        tzdt.GetEndOfDay().Ticks.Should().Be(tzdt.GetStartOfNextDay().Ticks - 1);
    }

    [Fact]
    public void GetEndOfDay_should_be_one_tick_before_start_of_next_day_for_tehran()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 10, 20, 30, Tehran);

        tzdt.GetEndOfDay().Ticks.Should().Be(tzdt.GetStartOfNextDay().Ticks - 1);
    }

    [Fact]
    public void GetEndOfHour_should_be_one_tick_before_start_of_next_hour_for_utc()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc).AddTicks(1234567), LocalTimezone.Utc);

        tzdt.GetEndOfHour().Ticks.Should().Be(tzdt.GetStartOfNextHour().Ticks - 1);
    }

    [Fact]
    public void GetEndOfMonth_should_be_one_tick_before_start_of_next_month_for_utc()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc), LocalTimezone.Utc);

        tzdt.GetEndOfMonth().Ticks.Should().Be(tzdt.GetStartOfNextMonth().Ticks - 1);
    }

    [Fact]
    public void GetEndOfMonth_should_be_one_tick_before_start_of_next_month_for_tehran()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 10, 20, 30, Tehran);

        tzdt.GetEndOfMonth().Ticks.Should().Be(tzdt.GetStartOfNextMonth().Ticks - 1);
    }

    // ---------------------------------------------------------------
    // A9 — GetEndOfWeek must not drift across DST weeks. Adding a fixed
    // 7x24h duration bleeds one hour into the next week when the week
    // contains a spring-forward transition.
    // ---------------------------------------------------------------

    [Fact]
    public void GetEndOfWeek_should_stay_within_the_week_across_dst_transition()
    {
        // Persian 1401-01-01 (Mon 2022-03-21). Week: Sat 03-19 .. Fri 03-25.
        // DST starts 2022-03-22, so the week is 167 hours long, not 168.
        var tzdt = new TimezoneDateTime(1401, 1, 1, 12, 0, 0, Tehran);

        var endOfWeek = tzdt.GetEndOfWeek();

        endOfWeek.Day.Should().Be(5); // Farvardin 5 = Friday 2022-03-25
        endOfWeek.Ticks.Should().Be(tzdt.GetStartOfNextWeek().Ticks - 1);
    }

    // ---------------------------------------------------------------
    // A5 — Humanize must never use the host machine's timezone. The
    // "today/yesterday at" branch used DateTime.ToLocalTime() for
    // UTC-timezone values.
    // ---------------------------------------------------------------

    [Fact]
    public void Humanize_today_at_should_use_the_value_timezone_not_the_host_timezone()
    {
        // Value 00:30Z, compared against 23:45Z the same UTC day -> "today at".
        // Pre-fix, a non-UTC host (e.g. Europe/Istanbul) printed the host-local time 03:30 AM.
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 0, 30, 0, DateTimeKind.Utc), LocalTimezone.Utc);
        var compareAgainst = new DateTime(2024, 1, 15, 23, 45, 0, DateTimeKind.Utc);

        var result = tzdt.Humanize(compareAgainst, maxRelativity: null);

        result.Should().Be("today at 12:30 AM");
    }

    // ---------------------------------------------------------------
    // A18 — Humanize future 6-24h: "tomorrow at" only when the target
    // actually falls on the next calendar day.
    // ---------------------------------------------------------------

    [Fact]
    public void Humanize_should_not_say_tomorrow_for_a_future_time_on_the_same_day()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);
        var compareAgainst = new DateTime(2024, 1, 15, 1, 0, 0, DateTimeKind.Utc);

        var result = tzdt.Humanize(compareAgainst, maxRelativity: null);

        result.Should().Be("in 8 hours");
    }

    [Fact]
    public void Humanize_should_say_tomorrow_for_a_future_time_on_the_next_day()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 16, 10, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);
        var compareAgainst = new DateTime(2024, 1, 15, 20, 0, 0, DateTimeKind.Utc);

        var result = tzdt.Humanize(compareAgainst, maxRelativity: null);

        result.Should().Be("tomorrow at 10:00 AM");
    }

    // ---------------------------------------------------------------
    // A6 — JSON converter host-independence and precision.
    // ---------------------------------------------------------------

    [Fact]
    public void Json_deserialize_should_treat_offsetless_strings_as_utc_regardless_of_host_timezone()
    {
        var deserialized = JsonSerializer.Deserialize<TimezoneDateTime>("\"2024-01-15T10:00:00\"");

        deserialized.GetUtcDateTime().Should().Be(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Json_roundtrip_should_preserve_subsecond_precision()
    {
        var dateTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc).AddMilliseconds(123);
        var tzdt = TimezoneDateTime.FromDateTime(dateTime, LocalTimezone.Utc);

        var deserialized = JsonSerializer.Deserialize<TimezoneDateTime>(JsonSerializer.Serialize(tzdt));

        deserialized.Should().Be(tzdt);
        deserialized.Millisecond.Should().Be(123);
    }

    [Fact]
    public void Json_serialize_should_keep_the_compact_format_for_whole_seconds()
    {
        var tzdt = TimezoneDateTime.FromDateTime(new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        JsonSerializer.Serialize(tzdt).Should().Be("\"2023-10-01T12:00:00Z\"");
    }

    // ---------------------------------------------------------------
    // A12 — Constructors must honor DateTimeKind.Local instead of
    // silently reinterpreting local ticks as UTC.
    // ---------------------------------------------------------------

    [Fact]
    public void Ctor_should_convert_local_kind_datetimes_to_utc()
    {
        var local = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);

        var tzdt = new TimezoneDateTime(local, LocalTimezone.Utc);

        tzdt.GetUtcDateTime().Should().Be(local.ToUniversalTime());
    }

    // ---------------------------------------------------------------
    // A14 — Millisecond argument must be validated, not silently clamped.
    // ---------------------------------------------------------------

    [Fact]
    public void Ctor_should_throw_when_millisecond_is_out_of_range()
    {
        var act = () => new TimezoneDateTime(2024, 1, 15, 10, 0, 0, 1500, LocalTimezone.Utc);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------
    // A4 — IsToday must compare calendar dates in the value's timezone.
    // The UTC branch used the host machine's DateTime.Today. The
    // deterministic overload takes the current UTC instant explicitly.
    // ---------------------------------------------------------------

    [Fact]
    public void IsToday_should_compare_dates_in_the_value_timezone()
    {
        // 2024-01-15T21:30Z = 2024-01-16 01:00 in Tehran (+03:30).
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 21, 30, 0, DateTimeKind.Utc), Tehran);

        // 2024-01-16T02:00Z = 2024-01-16 05:30 Tehran: same Tehran day, different UTC day.
        tzdt.IsToday(new DateTime(2024, 1, 16, 2, 0, 0, DateTimeKind.Utc)).Should().BeTrue();

        // 2024-01-16T22:00Z = 2024-01-17 01:30 Tehran: next Tehran day.
        tzdt.IsToday(new DateTime(2024, 1, 16, 22, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    [Fact]
    public void IsToday_should_use_utc_dates_for_utc_values()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 23, 30, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        tzdt.IsToday(new DateTime(2024, 1, 15, 0, 10, 0, DateTimeKind.Utc)).Should().BeTrue();
        tzdt.IsToday(new DateTime(2024, 1, 16, 0, 10, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // A11 — TimeSpan.WithTimezone must actually convert the wall-clock
    // time-of-day between timezones (it used to return its input).
    // ---------------------------------------------------------------

    [Fact]
    public void TimeSpan_WithTimezone_should_convert_wall_clock_between_timezones()
    {
        // 10:00 in Tehran (+03:30) is 06:30 in UTC.
        var converted = new TimeSpan(10, 0, 0).WithTimezone(Tehran, LocalTimezone.Utc);

        converted.Should().Be(new TimeSpan(6, 30, 0));
    }

    // ---------------------------------------------------------------
    // P4 — Arithmetic on the UTC fast path must not allocate. The Apply
    // overloads used value-capturing lambdas (2 closures per call).
    // ---------------------------------------------------------------

    [Fact]
    public void AddDays_on_utc_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        // Warm up so JIT/tiering allocations don't pollute the measurement.
        for (var i = 0; i < 2_000; i++)
            _ = tzdt.AddDays(3);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            _ = tzdt.AddDays(3);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().BeLessThan(50_000);
    }
}
