namespace R8.TzDateTime.Tests;

/// <summary>
///     Edge-case coverage for TimezoneDateTime: DST transitions on boundary operations, calendar
///     clamping, historical offsets, default values, operators, and JSON corner cases.
/// </summary>
public class TimezoneDateTimeEdgeCaseTests
{
    private static LocalTimezone Tehran => LocalTimezone.GetTimezone("Asia/Tehran");
    private static LocalTimezone Istanbul => LocalTimezone.GetTimezone("Europe/Istanbul");

    // ---------------------------------------------------------------
    // DST transitions on boundary operations (Iran, spring 2022:
    // midnight 2022-03-22 jumps to 01:00; Persian date 1401-01-02).
    // ---------------------------------------------------------------

    [Fact]
    public void GetStartOfDay_should_resolve_to_first_valid_time_when_midnight_is_skipped()
    {
        // Noon on the skipped-midnight day: 1401-01-02 12:00 +04:30 = 2022-03-22T07:30Z.
        var tzdt = new TimezoneDateTime(1401, 1, 2, 12, 0, 0, Tehran);

        var startOfDay = tzdt.GetStartOfDay();

        // Midnight does not exist; the day starts at 01:00 +04:30 = 2022-03-21T20:30Z.
        startOfDay.GetUtcDateTime().Should().Be(new DateTime(2022, 3, 21, 20, 30, 0, DateTimeKind.Utc));
        startOfDay.Hour.Should().Be(1);
        startOfDay.Day.Should().Be(2);
    }

    [Fact]
    public void Date_property_should_equal_GetStartOfDay()
    {
        var tzdt = new TimezoneDateTime(1401, 1, 2, 12, 0, 0, Tehran);

        tzdt.Date.Should().Be(tzdt.GetStartOfDay());
    }

    [Fact]
    public void GetEndOfDay_should_abut_the_next_day_across_a_dst_gap()
    {
        // 1401-01-01 (2022-03-21): its end must be one tick before the (shifted) start of 1401-01-02.
        var tzdt = new TimezoneDateTime(1401, 1, 1, 12, 0, 0, Tehran);

        var endOfDay = tzdt.GetEndOfDay();

        endOfDay.Ticks.Should().Be(tzdt.GetStartOfNextDay().Ticks - 1);
        // Start of next day is 01:00 +04:30 because midnight is skipped.
        tzdt.GetStartOfNextDay().GetUtcDateTime().Should().Be(new DateTime(2022, 3, 21, 20, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AddHours_should_use_elapsed_time_semantics_across_a_dst_gap()
    {
        // 1401-01-01 23:30 +03:30 = 2022-03-21T20:00Z. One elapsed hour later the wall clock
        // shows 01:30 +04:30 (2022-03-21T21:00Z) — 00:xx never happens.
        var tzdt = new TimezoneDateTime(1401, 1, 1, 23, 30, 0, Tehran);

        var result = tzdt.AddHours(1);

        result.GetUtcDateTime().Should().Be(new DateTime(2022, 3, 21, 21, 0, 0, DateTimeKind.Utc));
        result.Day.Should().Be(2);
        result.Hour.Should().Be(1);
        result.Minute.Should().Be(30);
    }

    [Fact]
    public void Time_parts_should_reflect_the_half_hour_dst_offset()
    {
        // During Iran DST the offset is +04:30: 2022-06-01T08:15Z = 12:45 local.
        var tzdt = new TimezoneDateTime(new DateTime(2022, 6, 1, 8, 15, 0, DateTimeKind.Utc), Tehran);

        tzdt.Hour.Should().Be(12);
        tzdt.Minute.Should().Be(45);

        var startOfHour = tzdt.GetStartOfHour();
        startOfHour.Minute.Should().Be(0);
        startOfHour.Hour.Should().Be(12);
        startOfHour.GetUtcDateTime().Should().Be(new DateTime(2022, 6, 1, 7, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void IsDaylightSavingTime_should_be_true_only_during_historical_dst()
    {
        var duringDst = new TimezoneDateTime(new DateTime(2022, 6, 1, 12, 0, 0, DateTimeKind.Utc), Tehran);
        var afterAbolition = new TimezoneDateTime(new DateTime(2023, 6, 1, 12, 0, 0, DateTimeKind.Utc), Tehran);
        var utc = new TimezoneDateTime(new DateTime(2022, 6, 1, 12, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        duringDst.IsDaylightSavingTime().Should().BeTrue();
        afterAbolition.IsDaylightSavingTime().Should().BeFalse();
        utc.IsDaylightSavingTime().Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Calendar clamping and month lengths.
    // ---------------------------------------------------------------

    [Fact]
    public void AddMonths_should_clamp_day_to_target_month_length_in_persian_calendar()
    {
        // Shahrivar (month 6) has 31 days; Mehr (month 7) has 30.
        var tzdt = new TimezoneDateTime(1401, 6, 31, 12, 0, 0, Tehran);

        var result = tzdt.AddMonths(1);

        (result.Year, result.Month, result.Day).Should().Be((1401, 7, 30));
    }

    [Fact]
    public void AddMonths_should_clamp_day_to_target_month_length_in_gregorian_calendar()
    {
        var tzdt = new TimezoneDateTime(2023, 1, 31, 12, 0, 0, Istanbul);

        var result = tzdt.AddMonths(1);

        (result.Year, result.Month, result.Day).Should().Be((2023, 2, 28));
    }

    [Theory]
    [InlineData(1399, 12, 30)] // leap year: Esfand has 30 days
    [InlineData(1400, 12, 29)] // non-leap year: Esfand has 29 days
    [InlineData(1401, 6, 31)] // Shahrivar always has 31 days
    public void GetDaysInMonth_should_follow_the_persian_calendar(int year, int month, int expectedDays)
    {
        var tzdt = new TimezoneDateTime(year, month, 1, 12, 0, 0, Tehran);

        tzdt.GetDaysInMonth().Should().Be(expectedDays);
    }

    [Fact]
    public void Persian_year_should_flip_exactly_at_the_local_new_year_instant()
    {
        // 1403-01-01 begins 2024-03-20 00:00 +03:30 = 2024-03-19T20:30Z.
        var justBefore = new TimezoneDateTime(new DateTime(2024, 3, 19, 20, 29, 59, DateTimeKind.Utc), Tehran);
        var justAfter = new TimezoneDateTime(new DateTime(2024, 3, 19, 20, 30, 0, DateTimeKind.Utc), Tehran);

        justBefore.Year.Should().Be(1402);
        justAfter.Year.Should().Be(1403);
        justAfter.Month.Should().Be(1);
        justAfter.Day.Should().Be(1);
    }

    // ---------------------------------------------------------------
    // Timezone conversion and aliases.
    // ---------------------------------------------------------------

    [Fact]
    public void WithTimezone_should_keep_the_instant_and_change_the_wall_clock()
    {
        var utcNoon = new TimezoneDateTime(new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        var tehran = utcNoon.WithTimezone(Tehran);

        tehran.Should().Be(utcNoon); // equality is instant-based
        tehran.GetHashCode().Should().Be(utcNoon.GetHashCode());
        tehran.Hour.Should().Be(15);
        tehran.Minute.Should().Be(30);
        tehran.Year.Should().Be(1402); // Persian calendar
    }

    [Fact]
    public void Utc_aliases_should_behave_identically_to_the_default_utc_id()
    {
        var dateTime = new DateTime(2024, 1, 7, 15, 0, 0, DateTimeKind.Utc); // a Sunday
        var viaUtc = new TimezoneDateTime(dateTime, LocalTimezone.GetTimezone("UTC"));
        var viaAlias = new TimezoneDateTime(dateTime, LocalTimezone.GetTimezone("Etc/UTC"));

        viaAlias.Year.Should().Be(viaUtc.Year);
        viaAlias.GetStartOfWeek().Ticks.Should().Be(viaUtc.GetStartOfWeek().Ticks);
        viaAlias.GetStartOfNextWeek().Ticks.Should().Be(viaUtc.GetStartOfNextWeek().Ticks);
        viaAlias.GetTimezone().Equals(viaUtc.GetTimezone()).Should().BeTrue();
    }

    [Fact]
    public void Utc_week_should_run_monday_through_sunday()
    {
        // Wednesday 2024-01-10: ISO-8601 week is Mon 2024-01-08 .. Sun 2024-01-14.
        var tzdt = new TimezoneDateTime(new DateTime(2024, 1, 10, 15, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);

        var startOfWeek = tzdt.GetStartOfWeek();
        var endOfWeek = tzdt.GetEndOfWeek();

        (startOfWeek.Year, startOfWeek.Month, startOfWeek.Day).Should().Be((2024, 1, 8));
        startOfWeek.DayOfWeek.Should().Be(DayOfWeek.Monday);
        (endOfWeek.Year, endOfWeek.Month, endOfWeek.Day).Should().Be((2024, 1, 14));
        endOfWeek.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    // ---------------------------------------------------------------
    // Default value and Empty.
    // ---------------------------------------------------------------

    [Fact]
    public void Default_value_should_behave_like_Empty()
    {
        var defaultValue = default(TimezoneDateTime);

        defaultValue.Should().Be(TimezoneDateTime.Empty);
        defaultValue.Ticks.Should().Be(0);
        defaultValue.Year.Should().Be(1);
        defaultValue.GetTimezone().DefaultIanaId.Should().Be("UTC");
        defaultValue.ToString().Should().NotBeNullOrEmpty();
    }

    // ---------------------------------------------------------------
    // Operators and arithmetic.
    // ---------------------------------------------------------------

    [Fact]
    public void Comparison_operators_should_order_by_instant()
    {
        var earlier = new TimezoneDateTime(new DateTime(2023, 10, 1, 10, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);
        var later = new TimezoneDateTime(new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), Tehran);
        var sameAsEarlier = new TimezoneDateTime(new DateTime(2023, 10, 1, 10, 0, 0, DateTimeKind.Utc), Tehran);

        (earlier < later).Should().BeTrue();
        (later > earlier).Should().BeTrue();
        (earlier <= sameAsEarlier).Should().BeTrue();
        (earlier >= sameAsEarlier).Should().BeTrue();
        (earlier == sameAsEarlier).Should().BeTrue(); // cross-timezone, same instant
        (earlier != later).Should().BeTrue();
        earlier.CompareTo(later).Should().BeNegative();
        earlier.CompareTo((object)later).Should().BeNegative();
        earlier.CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void CompareTo_should_throw_for_foreign_types()
    {
        var tzdt = TimezoneDateTime.Empty;

        var act = () => tzdt.CompareTo("not a TimezoneDateTime");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TimeSpan_operators_should_round_trip()
    {
        var tzdt = new TimezoneDateTime(new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), Tehran);
        var offset = TimeSpan.FromHours(5.5);

        var forward = tzdt + offset;
        var back = forward - offset;

        back.Should().Be(tzdt);
        (forward - tzdt).Should().Be(offset);
        forward.GetTimezone().DefaultIanaId.Should().Be(tzdt.GetTimezone().DefaultIanaId);
    }

    // ---------------------------------------------------------------
    // JSON corner cases.
    // ---------------------------------------------------------------

    [Fact]
    public void Json_should_round_trip_nullable_values()
    {
        TimezoneDateTime? value = new TimezoneDateTime(new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc), LocalTimezone.Utc);
        TimezoneDateTime? nullValue = null;

        JsonSerializer.Deserialize<TimezoneDateTime?>(JsonSerializer.Serialize(value)).Should().Be(value);
        JsonSerializer.Serialize(nullValue).Should().Be("null");
        JsonSerializer.Deserialize<TimezoneDateTime?>("null").Should().BeNull();
    }

    [Fact]
    public void Json_should_throw_for_non_string_tokens_on_nullable_values()
    {
        var act = () => JsonSerializer.Deserialize<TimezoneDateTime?>("123");

        act.Should().Throw<JsonException>().WithMessage("The value is expected to be a string, but was Number");
    }

    [Fact]
    public void Json_should_honor_explicit_utc_offsets()
    {
        var deserialized = JsonSerializer.Deserialize<TimezoneDateTime>("\"2023-10-01T12:00:00+03:30\"");

        deserialized.GetUtcDateTime().Should().Be(new DateTime(2023, 10, 1, 8, 30, 0, DateTimeKind.Utc));
    }

    // ---------------------------------------------------------------
    // Formatting.
    // ---------------------------------------------------------------

    [Fact]
    public void ToString_should_prefer_an_explicit_culture_over_the_timezone_culture()
    {
        var tzdt = new TimezoneDateTime(1402, 7, 10, 14, 30, 0, Tehran);

        var invariant = tzdt.ToString("HH:mm", CultureInfo.InvariantCulture);

        invariant.Should().Be("14:30");
    }

    // ---------------------------------------------------------------
    // Guards.
    // ---------------------------------------------------------------

    [Fact]
    public void Humanize_should_reject_non_utc_comparands()
    {
        var tzdt = TimezoneDateTime.Now;

        var act = () => tzdt.Humanize(DateTime.Now, maxRelativity: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsToday_should_reject_non_utc_instants()
    {
        var tzdt = TimezoneDateTime.Now;

        var act = () => tzdt.IsToday(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToTimezoneDateTime_should_reject_min_and_max_values()
    {
        var minAct = () => DateTime.MinValue.ToTimezoneDateTime(Tehran);
        var maxAct = () => DateTime.MaxValue.ToTimezoneDateTime(Tehran);

        minAct.Should().Throw<ArgumentOutOfRangeException>();
        maxAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TimeSpan_WithTimezone_should_convert_in_both_directions()
    {
        new TimeSpan(6, 30, 0).WithTimezone(LocalTimezone.Utc, Tehran).Should().Be(new TimeSpan(10, 0, 0));
        new TimeSpan(10, 0, 0).WithTimezone(Tehran, Tehran).Should().Be(new TimeSpan(10, 0, 0));
    }
}
