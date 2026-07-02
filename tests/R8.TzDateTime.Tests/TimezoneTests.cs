namespace R8.TzDateTime.Tests;

public class TimezoneTests
{
    // public TimezoneTests()
    // {
    //     // LocalTimezone.Mappings.Clear();
    //     LocalTimezone.AddTimezone<IranTimezone>();
    //     LocalTimezone.AddTimezone<TurkeyTimezone>();
    //     LocalTimezone.AddTimezone<UKTimezone>();
    //     LocalTimezone.AddTimezone<LosAngelesTimezone>();
    // }

    [Fact]
    public void should_return_utc_timezone()
    {
        var timezone = LocalTimezone.Utc;
        var currentTimezone = DateTimeZone.Utc;

        timezone.IanaIds.Should().Contain(currentTimezone.Id);
    }

    [Fact]
    public void should_return_timezone_from_resolver()
    {
        // Act
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        // Assert
        timezone.Should().NotBeNull();
        timezone.IanaIds.Should().Contain("Asia/Tehran");
        timezone.GetSystemTimeZone().StandardName.Should().BeOneOf("Iran Standard Time", "Iran Daylight Time");
        timezone.ToString().Should().Be("GMT+03:30");
        timezone.Culture.Should().Be(CultureInfo.GetCultureInfo("fa-IR"));
        timezone.Calendar.Should().Be(CalendarSystem.PersianSimple);

        var daysOfWeek = timezone.DaysOfWeek;
        daysOfWeek[0].Should().Be(DayOfWeek.Saturday);
        daysOfWeek[1].Should().Be(DayOfWeek.Sunday);
        daysOfWeek[2].Should().Be(DayOfWeek.Monday);
        daysOfWeek[3].Should().Be(DayOfWeek.Tuesday);
        daysOfWeek[4].Should().Be(DayOfWeek.Wednesday);
        daysOfWeek[5].Should().Be(DayOfWeek.Thursday);
        daysOfWeek[6].Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public void should_return_timezone_of_iran()
    {
        // Act
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        // Assert
        timezone.IanaIds.Should().Contain("Asia/Tehran");
        timezone.GetSystemTimeZone().StandardName.Should().BeOneOf("Iran Standard Time", "Iran Daylight Time");
        timezone.ToString().Should().Be("GMT+03:30");
        timezone.Culture.Should().Be(CultureInfo.GetCultureInfo("fa-IR"));
        timezone.Calendar.Should().Be(CalendarSystem.PersianSimple);

        var daysOfWeek = timezone.DaysOfWeek;
        daysOfWeek[0].Should().Be(DayOfWeek.Saturday);
        daysOfWeek[1].Should().Be(DayOfWeek.Sunday);
        daysOfWeek[2].Should().Be(DayOfWeek.Monday);
        daysOfWeek[3].Should().Be(DayOfWeek.Tuesday);
        daysOfWeek[4].Should().Be(DayOfWeek.Wednesday);
        daysOfWeek[5].Should().Be(DayOfWeek.Thursday);
        daysOfWeek[6].Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public void should_return_timezone_of_turkey()
    {
        // Act
        var timezone = LocalTimezone.GetTimezone("Europe/Istanbul");

        // Assert
        timezone.IanaIds.Should().Contain("Europe/Istanbul");
        timezone.ToString().Should().Be("GMT+03:00");
        timezone.Culture.Should().Be(CultureInfo.GetCultureInfo("tr-TR"));
        timezone.Calendar.Should().Be(CalendarSystem.Gregorian);

        var daysOfWeek = timezone.DaysOfWeek;
        daysOfWeek[0].Should().Be(DayOfWeek.Monday);
        daysOfWeek[1].Should().Be(DayOfWeek.Tuesday);
        daysOfWeek[2].Should().Be(DayOfWeek.Wednesday);
        daysOfWeek[3].Should().Be(DayOfWeek.Thursday);
        daysOfWeek[4].Should().Be(DayOfWeek.Friday);
        daysOfWeek[5].Should().Be(DayOfWeek.Saturday);
        daysOfWeek[6].Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public void should_return_cached_timezone()
    {
        // Act
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        // Assert
        timezone.IanaIds.Should().Contain("Asia/Tehran");
        timezone.GetSystemTimeZone().StandardName.Should().BeOneOf("Iran Standard Time", "Iran Daylight Time");
        timezone.ToString().Should().Be("GMT+03:30");
        timezone.Culture.Should().Be(CultureInfo.GetCultureInfo("fa-IR"));
        timezone.Calendar.Should().Be(CalendarSystem.PersianSimple);

        var daysOfWeek = timezone.DaysOfWeek;
        daysOfWeek[0].Should().Be(DayOfWeek.Saturday);
        daysOfWeek[1].Should().Be(DayOfWeek.Sunday);
        daysOfWeek[2].Should().Be(DayOfWeek.Monday);
        daysOfWeek[3].Should().Be(DayOfWeek.Tuesday);
        daysOfWeek[4].Should().Be(DayOfWeek.Wednesday);
        daysOfWeek[5].Should().Be(DayOfWeek.Thursday);
        daysOfWeek[6].Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public void should_be_equal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Asia/Tehran");

        timezone.Should().Be(timezone2);
    }

    [Fact]
    public void should_not_be_equal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Europe/Istanbul");

        timezone.Should().NotBe(timezone2);
    }

    [Fact]
    public void should_not_be_equal_with_null()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");

        timezone.Should().NotBeNull();
    }

    [Fact]
    public void should_be_equal_by_operator()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Asia/Tehran");

        (timezone == timezone2).Should().BeTrue();
    }

    [Fact]
    public void should_not_be_equal_by_operator()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Europe/Istanbul");

        (timezone != timezone2).Should().BeTrue();
    }

    [Fact]
    public void should_be_casted_to_string()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        ((string)timezone).Should().Be("Asia/Tehran");
    }

    [Fact]
    public void should_be_casted_to_timezone()
    {
        var timezone = (LocalTimezone)"Asia/Tehran";
        timezone.IanaIds.Should().Contain("Asia/Tehran");
    }

    [Fact]
    public void should_return_offset_from_timezone()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var offset = timezone.Offset;

        offset.Should().Be(Offset.FromHoursAndMinutes(3, 30));
    }

    [Fact]
    public void should_be_comparable_greater_than()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Europe/Istanbul");

        (timezone > timezone2).Should().BeTrue();
    }

    [Fact]
    public void should_be_comparable_greater_than_or_equal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Asia/Tehran");

        (timezone >= timezone2).Should().BeTrue();
    }

    [Fact]
    public void should_be_comparable_less_than()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Europe/Istanbul");

        (timezone2 < timezone).Should().BeTrue();
    }

    [Fact]
    public void should_be_comparable_less_than_or_equal()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Asia/Tehran");

        (timezone2 <= timezone).Should().BeTrue();
    }

    [Fact]
    public void should_be_comparable_hash_code()
    {
        var timezone = LocalTimezone.GetTimezone("Asia/Tehran");
        var timezone2 = LocalTimezone.GetTimezone("Asia/Tehran");

        timezone2.GetHashCode().Should().Be(timezone.GetHashCode());
    }
}