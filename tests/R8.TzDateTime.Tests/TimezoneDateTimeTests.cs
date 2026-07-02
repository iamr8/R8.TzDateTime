namespace R8.TzDateTime.Tests;

public class TimezoneDateTimeTests
{
    private readonly ITestOutputHelper _outputHelper;

    public TimezoneDateTimeTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    // [Fact]
    // public void benchmark()
    // {
    //     _outputHelper.WriteLine("Warming up...");
    //     var original = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
    //     for (var i = 0; i < 1_000; i++)
    //     {
    //         original.GetStartOfMonth();
    //         original.GetStartOfMonth2();
    //     }
    //
    //     _outputHelper.WriteLine("Start benchmarking...");
    //     _outputHelper.WriteLine($"Original: {original.ToString()}");
    //
    //     var sw = Stopwatch.StartNew();
    //     var manual = original.GetStartOfMonth();
    //     sw.Stop();
    //     _outputHelper.WriteLine($"Manual Elapsed: {sw.Elapsed.TotalMilliseconds} ms");
    //     _outputHelper.WriteLine($"Manual: {manual.ToString()}");
    //
    //     sw.Restart();
    //     var builtin = original.GetStartOfMonth2();
    //     sw.Stop();
    //     _outputHelper.WriteLine($"Builtin Elapsed: {sw.Elapsed.TotalMilliseconds} ms");
    //     _outputHelper.WriteLine($"Builtin: {manual.ToString()}");
    //
    //     Assert.Equal(manual.Year, builtin.Year);
    //     Assert.Equal(manual.Month, builtin.Month);
    //     Assert.Equal(manual.Day, builtin.Day);
    //     Assert.Equal(manual.Hour, builtin.Hour);
    //     Assert.Equal(manual.Minute, builtin.Minute);
    //     Assert.Equal(manual.Second, builtin.Second);
    //     Assert.Equal(manual.Millisecond, builtin.Millisecond);
    // }

    [Fact]
    public void should()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var tzdt1 = new TimezoneDateTime(1402, 12, 1, 0, 0, 0, timezone);
        var tzdt2 = new TimezoneDateTime(1402, 12, 29, 23, 59, 59, timezone);

        _outputHelper.WriteLine(tzdt1.GetUtcDateTime().ToString());
        _outputHelper.WriteLine(tzdt2.GetUtcDateTime().ToString());
    }

    [Fact]
    public void should_return_tzdt_from_datetime()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        var result = dateTime.ToTimezoneDateTime(timezone);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);

        result.DayOfWeek.Should().Be(DayOfWeek.Friday);

        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_datetime_according_to_current_timezone()
    {
        LocalTimezone.Current = LocalTimezone.GetTimezone("Asia/Tehran")!;

        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.Current);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        result.DayOfWeek.Should().Be(DayOfWeek.Friday);

        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_start_of_week_when_current_day_is_in_middle_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 24, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(21, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_start_of_week_when_current_day_is_in_middle_of_week2()
    {
        var result = new TimezoneDateTime(1404, 01, 11, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfWeek();

        Assert.Equal(1404, result.Year);
        Assert.Equal(01, result.Month);
        Assert.Equal(09, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_start_of_week_when_current_day_is_first_day_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 21, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(21, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_start_of_week_when_current_day_is_last_day_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 27, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(21, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_end_of_week_when_current_day_is_first_day_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 21, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(27, result.Day);

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_end_of_week_when_current_day_is_last_day_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 27, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(27, result.Day);

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
    }

    [Fact]
    public void should_return_tzdt_from_end_of_week_when_current_day_is_in_middle_of_week()
    {
        var result = new TimezoneDateTime(1402, 11, 24, 23, 50, 11, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfWeek();

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(27, result.Day);

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
    }

    [Fact]
    public void should_return_tzdt_with_start_of_hour()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfHour();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_end_of_hour()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfHour();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_start_of_minute()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfMinute();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_end_of_minute()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfMinute();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(59, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_start_of_day()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfDay();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_end_of_day()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfDay();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_start_of_month()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetStartOfMonth();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(1, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_end_of_month()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.GetEndOfMonth();

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(30, result.Day);

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_local_date_time()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_local_date()
    {
        var result = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_datetime2()
    {
        var dateTime = new DateTime(2021, 3, 21, 12, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.Equal(1400, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(1, result.Day);

        Assert.Equal(15, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(31, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_from_datetime3()
    {
        var dateTime = new DateTime(2021, 3, 21, 23, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.Equal(1400, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(2, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(31, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_minute()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMinutes(-1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(29, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_minute()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMinutes(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(31, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_second()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddSeconds(-1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(29, result.Minute);
        Assert.Equal(59, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_second()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddSeconds(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(1, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_second_resulted_to_next_minute()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 4, 0, 59, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddSeconds(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(4, result.Hour);
        Assert.Equal(1, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_return_tzdt_with_different_timezone()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 30, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.WithTimezone(LocalTimezone.GetTimezone("Europe/Istanbul"));

        Assert.Equal(2021, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(1, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_add_minute_resulted_to_next_hour()
    {
        var result = new TimezoneDateTime(1399, 10, 12, 3, 59, 0, LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMinutes(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(4, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_compare_to_other_while_are_equal()
    {
        var result = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.Equal(0, result.CompareTo(result2));
    }

    [Fact]
    public void should_add_month()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_month2()
    {
        var dateTime = new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(13, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_year()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var result = new TimezoneDateTime(1403, 05, 29, 13, 33, 0, timezone!);
        result = result.AddYears(1);

        Assert.Equal(1404, result.Year);
        Assert.Equal(05, result.Month);
        Assert.Equal(29, result.Day);

        Assert.Equal(13, result.Hour);
        Assert.Equal(33, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_add_years()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var result = new TimezoneDateTime(1403, 05, 29, 0, 0, 0, timezone!);
        result = result.AddYears(3);

        Assert.Equal(1406, result.Year);
        Assert.Equal(05, result.Month);
        Assert.Equal(29, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_add_year_when_current_year_is_LeapYear()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var result = new TimezoneDateTime(1404, 05, 29, 0, 0, 0, timezone!);
        result = result.AddYears(1);

        Assert.Equal(1405, result.Year);
        Assert.Equal(05, result.Month);
        Assert.Equal(29, result.Day);

        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_subtract_year()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc); // 1397
        var currentYear = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        var result = currentYear.AddYears(-2);

        Assert.Equal(1397, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_day()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var expected = dateTime.ToTimezoneDateTime(timezone);
        var actual = expected.AddDays(1);

        Assert.Equal(1399, actual.Year);
        Assert.Equal(10, actual.Month);
        Assert.Equal(13, actual.Day);

        Assert.Equal(3, actual.Hour);
        Assert.Equal(30, actual.Minute);
        Assert.Equal(0, actual.Second);


        Assert.Equal(30, actual.GetDaysInMonth());
    }

    [Fact]
    public void should_add()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1404, 02, 19, 16, 23, 0, timezone);

        var actual = dateTime.Add(TimeSpan.FromMinutes(75));

        Assert.Equal(1404, actual.Year);
        Assert.Equal(02, actual.Month);
        Assert.Equal(19, actual.Day);

        Assert.Equal(17, actual.Hour);
        Assert.Equal(38, actual.Minute);
        Assert.Equal(0, actual.Second);
    }

    [Fact]
    public void should_add_days_resulted_to_new_month()
    {
        var dateTime = new DateTime(2021, 1, 20, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(1, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_hour()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddHours(1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(4, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_hour()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddHours(-1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(2, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_be_same_as_local_values()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        var actual = new TimezoneDateTime(1399, 10, 12, 23, 30, 0, timezone);
        actual.Year.Should().Be(1399);
        actual.Month.Should().Be(10);
        actual.Day.Should().Be(12);
        actual.Hour.Should().Be(23);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

    [Fact]
    public void should_add_hour_resulted_to_next_day()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        var actual = new TimezoneDateTime(1399, 10, 12, 23, 30, 0, timezone);
        actual = actual.AddHours(1);

        var expected = new TimezoneDateTime(1399, 10, 13, 0, 30, 0, timezone);
        actual.Should().Be(expected);

        actual.Year.Should().Be(expected.Year);
        actual.Month.Should().Be(expected.Month);
        actual.Day.Should().Be(expected.Day);
        actual.Hour.Should().Be(expected.Hour);
        actual.Minute.Should().Be(expected.Minute);
        actual.Second.Should().Be(expected.Second);
        actual.GetDaysInMonth().Should().Be(expected.GetDaysInMonth());
        actual.DayOfWeek.Should().Be(expected.DayOfWeek);
        actual.GetUtcDateTime().Should().Be(expected.GetUtcDateTime());
    }

    [Fact]
    public void should_return_underlying_datetime()
    {
        var result = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        var underlyingDateTime = result.GetUtcDateTime();
        Assert.Equal(2020, underlyingDateTime.Year);
        Assert.Equal(12, underlyingDateTime.Month);
        Assert.Equal(31, underlyingDateTime.Day);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(12, result.Day);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_month()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(-1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(9, result.Month);
        Assert.Equal(12, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_twelve_months()
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(-12);

        Assert.Equal(1400, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(14, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_add_month3()
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddMonths(12);

        Assert.Equal(1402, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(14, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_subtract_day()
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddDays(-1);

        Assert.Equal(1399, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(11, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);


        Assert.Equal(30, result.GetDaysInMonth());
    }

    [Fact]
    public void should_be_equal()
    {
        var result = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result2 == result);
    }

    [Fact]
    public void should_not_be_equal()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result2 != result);
    }

    [Fact]
    public void should_be_greater_than()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result > result2);
    }

    [Fact]
    public void should_be_greater_than_or_equal_to()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result >= result2);
    }

    [Fact]
    public void should_be_less_than_or_equal_to()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result2 <= result);
    }

    [Fact]
    public void should_be_less_than()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result2 < result);
    }

    [Theory]
    [InlineData(null, "1401/11/14 3:30:00")]
    [InlineData("d", "1401/11/14")]
    [InlineData("D", "1401 بهمن 14, جمعه")]
    [InlineData("f", "1401 بهمن 14, جمعه 3:30")]
    [InlineData("F", "1401 بهمن 14, جمعه 3:30:00")]
    [InlineData("g", "1401/11/14 3:30")]
    [InlineData("G", "1401/11/14 3:30:00")]
    [InlineData("m", "14 بهمن")]
    [InlineData("M", "14 بهمن")]
    [InlineData("MMMM", "بهمن")]
    public void should_return_formatted_string(string? format, string expected)
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        var resultString = result.ToString(format);

        Assert.Equal(expected, resultString);
    }

    [Fact]
    public void should_return_formatted_ordinal_string()
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);


        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        var resultString = result.ToString();

        resultString.Should().Be("1401/11/14 3:30:00");
    }

    [Fact]
    public void should_subtract_two_tzdt_by_operator()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        var result3 = result - result2;

        Assert.Equal(1, result3.Days);
    }

    [Fact]
    public void should_subtract_two_tzdt_by_operator2()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        var result3 = result2 - result;

        Assert.Equal(-1, result3.Days);
    }

    [Fact]
    public void should_subtract_two_tzdt_by_Subtract_method()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        var result3 = result2.Subtract(result);

        Assert.Equal(-1, result3.Days);
    }

    [Fact]
    public void should_subtract_tzdt_and_TimeSpan_by_Subtract_method()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = result.Subtract(TimeSpan.FromDays(1));

        Assert.Equal(1399, result2.Year);
        Assert.Equal(10, result2.Month);
        Assert.Equal(12, result2.Day);

        Assert.Equal(0, result2.Hour);
        Assert.Equal(0, result2.Minute);
        Assert.Equal(0, result2.Second);
    }

    [Fact]
    public void should_add_tzdt_and_TimeSpan_by_Add_method()
    {
        var original = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var actual = original.Add(TimeSpan.FromDays(1));

        Assert.Equal(1399, actual.Year);
        Assert.Equal(10, actual.Month);
        Assert.Equal(14, actual.Day);

        Assert.Equal(0, actual.Hour);
        Assert.Equal(0, actual.Minute);
        Assert.Equal(0, actual.Second);
    }

    [Theory]
    [InlineData(1399, 10, 13, 1, 1399, 10, 14)]
    [InlineData(1399, 10, 30, 1, 1399, 11, 1)]
    public void should_add_tzdt_and_TimeSpan_by_operator(int year, int month, int day, int addingDays, int resultYear, int resultMonth, int resultDay)
    {
        var result = new TimezoneDateTime(year, month, day, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = result + TimeSpan.FromDays(addingDays);

        Assert.Equal(resultYear, result2.Year);
        Assert.Equal(resultMonth, result2.Month);
        Assert.Equal(resultDay, result2.Day);

        Assert.Equal(0, result2.Hour);
        Assert.Equal(0, result2.Minute);
        Assert.Equal(0, result2.Second);
    }

    [Fact]
    public void should_returns_ticks()
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);
        var tzdt = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));

        dateTime.Ticks.Should().Be(tzdt.Ticks);
    }

    [Fact]
    public void should_be_equal_by_Equal_method()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.True(result2.Equals(result));
    }

    [Fact]
    public void should_not_be_equal_by_Equal_method()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        Assert.False(result2.Equals(result));
    }

    [Fact]
    public void should_subtract_two_tzdt_by_operator3()
    {
        var result = new TimezoneDateTime(1399, 10, 13, LocalTimezone.GetTimezone("Asia/Tehran"));
        var result2 = new TimezoneDateTime(1399, 10, 12, LocalTimezone.GetTimezone("Asia/Tehran"));

        var result3 = result - result2;

        Assert.Equal(1, result3.Days);
    }

    [Fact]
    public void should_add_days()
    {
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);
        var result = dateTime.ToTimezoneDateTime(LocalTimezone.GetTimezone("Asia/Tehran"));
        result = result.AddDays(1);
        Assert.Equal(1401, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(15, result.Day);

        Assert.Equal(3, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_hour_from_DateTime()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1401, 11, 15, 1, 35, 23, timezone);
        var result = dateTime.GetStartOfNextHour();

        Assert.Equal(1401, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(15, result.Day);
        Assert.Equal(2, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_hour_from_DateTime_avoid_Ambiguous_Time()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1401, 06, 30, 22, 00, 00, timezone);
        var result = dateTime.GetStartOfNextHour();
        Assert.Equal(1401, result.Year);
        Assert.Equal(6, result.Month);
        Assert.Equal(30, result.Day);
        Assert.Equal(23, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_hour_when_next_hour_is_first_hour_in_next_month()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1401, 11, 30, 23, 35, 23, timezone);
        var result = dateTime.GetStartOfNextHour();

        Assert.Equal(1401, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(1, result.Day);
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_hour_when_next_hour_is_first_hour_in_next_year()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1401, 12, 29, 23, 35, 23, timezone);
        var result = dateTime.GetStartOfNextHour();

        Assert.Equal(1402, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(1, result.Day);
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_hour_when_next_hour_is_first_hour_in_next_day()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new TimezoneDateTime(1401, 11, 25, 23, 35, 23, timezone);
        var result = dateTime.GetStartOfNextHour();

        Assert.Equal(1401, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(26, result.Day);
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_day_from_DateTime()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var dateTime = new DateTime(2023, 2, 3, 0, 0, 0, DateTimeKind.Utc);
        var result = dateTime.ToTimezoneDateTime(timezone);
        result = result.GetStartOfNextDay();
        Assert.Equal(1401, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(15, result.Day);
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void should_return_start_of_next_day_from_DateTimeLocal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 11, 14, 18, 30, 0, timezone);
        result = result.GetStartOfNextDay();
        result.Year.Should().Be(1401);
        result.Month.Should().Be(11);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_day_of_next_month_from_DateTimeLocal_when_current_day_is_last_day_of_month()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 11, 30, 18, 30, 0, timezone);
        result = result.GetStartOfNextDay();
        result.Year.Should().Be(1401);
        result.Month.Should().Be(12);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_first_day_of_next_year_from_DateTimeLocal_when_current_day_is_last_day_of_year()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 12, 29, 18, 30, 0, timezone);
        result = result.GetStartOfNextDay();
        result.Year.Should().Be(1402);
        result.Month.Should().Be(1);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_month_from_DateTimeLocal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 11, 14, 18, 30, 0, timezone);
        result = result.GetStartOfNextMonth();
        result.Year.Should().Be(1401);
        result.Month.Should().Be(12);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_month_from_DateTimeLocal_when_current_month_is_last_month_of_year()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 12, 14, 18, 30, 0, timezone);
        result = result.GetStartOfNextMonth();
        result.Year.Should().Be(1402);
        result.Month.Should().Be(1);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_month_from_UTC()
    {
        var result = new TimezoneDateTime(2023, 2, 3, 0, 0, 0, LocalTimezone.Utc);
        result = result.GetStartOfNextMonth();
        result.Year.Should().Be(2023);
        result.Month.Should().Be(3);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_month_from_UTC_when_current_month_is_last_month_of_year()
    {
        var result = new TimezoneDateTime(2023, 12, 29, 0, 0, 0, LocalTimezone.Utc);
        result = result.GetStartOfNextMonth();
        result.Year.Should().Be(2024);
        result.Month.Should().Be(1);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_DateTimeLocal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 11, 14, 18, 30, 0, timezone);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(1401);
        result.Month.Should().Be(11);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_UTC()
    {
        var result = new TimezoneDateTime(2023, 2, 3, 0, 0, 0, LocalTimezone.Utc);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(2023);
        result.Month.Should().Be(2);
        result.Day.Should().Be(6);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_UTC_when_current_week_ends_in_next_month()
    {
        var result = new TimezoneDateTime(2023, 2, 28, 0, 0, 0, LocalTimezone.Utc);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(2023);
        result.Month.Should().Be(3);
        result.Day.Should().Be(6);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_UTC_when_current_week_ends_in_next_year()
    {
        var result = new TimezoneDateTime(2023, 12, 29, 0, 0, 0, LocalTimezone.Utc);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(2024);
        result.Month.Should().Be(1);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_DateTimeLocal_when_current_week_ends_in_next_month()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1403, 11, 30, 18, 30, 0, timezone);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(1403);
        result.Month.Should().Be(12);
        result.Day.Should().Be(4);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_start_of_next_week_from_DateTimeLocal_when_current_week_ends_in_next_year()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1403, 12, 29, 18, 30, 0, timezone);
        result = result.GetStartOfNextWeek();
        result.Year.Should().Be(1404);
        result.Month.Should().Be(1);
        result.Day.Should().Be(2);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void should_return_True_when_IsToday_Tehran()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var local = timezone.Clock.GetCurrentLocalDateTime();
        var ticks = timezone.Clock.Zone.AtStrictly(local).ToDateTimeUtc().Ticks;

        var result = new TimezoneDateTime(ticks, timezone);
        result.IsToday().Should().BeTrue();
    }

    [Fact]
    public void should_return_True_when_IsToday_UTC()
    {
        var timezone = LocalTimezone.GetTimezone("UTC");
        var local = timezone.Clock.GetCurrentLocalDateTime();
        var ticks = timezone.Clock.Zone.AtStrictly(local).ToDateTimeUtc().Ticks;

        var result = new TimezoneDateTime(ticks, timezone);
        result.IsToday().Should().BeTrue();
    }

    [Fact]
    public void should_return_false_when_Is_not_Today()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran")!;
        var result = new TimezoneDateTime(1401, 11, 15, 18, 30, 0, timezone);
        result.IsToday().Should().BeFalse();
    }

    [Fact]
    public void should_return_false_when_Is_not_Today_UTC()
    {
        var timezone = LocalTimezone.GetTimezone("UTC");
        var result = new TimezoneDateTime(2023, 2, 3, 18, 30, 0, timezone);
        result.IsToday().Should().BeFalse();
    }
}