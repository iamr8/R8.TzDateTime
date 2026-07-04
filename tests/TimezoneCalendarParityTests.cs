namespace R8.TzDateTime.Tests;

/// <summary>
///     Pins calendar arithmetic (AddYears/AddMonths/month boundaries) to NodaTime's lenient semantics
///     for every supported zone, so calendar-specific shortcuts (e.g. BCL Gregorian math) must produce
///     byte-identical results.
/// </summary>
public class TimezoneCalendarParityTests
{
    private const long BclUnixEpochTicks = 621355968000000000;

    public static IEnumerable<object[]> Cases()
    {
        var instants = new[]
        {
            new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc), // Gregorian leap day
            new DateTime(2023, 1, 31, 20, 30, 0, DateTimeKind.Utc), // month-length clamp
            new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc), // year boundary
            new DateTime(1995, 6, 10, 8, 0, 0, DateTimeKind.Utc), // historical DST era
            new DateTime(2015, 3, 28, 22, 30, 0, DateTimeKind.Utc), // lands near Istanbul spring-forward
            new DateTime(2021, 3, 19, 21, 0, 0, DateTimeKind.Utc), // Persian leap-year era
            new DateTime(2100, 5, 5, 5, 5, 5, DateTimeKind.Utc), // far future
        };

        // A fixed set (not the live, mutable registry) so other tests registering zones can't change the case count.
        var zoneIds = TestTimezones.Ids;

        foreach (var zoneId in zoneIds)
        foreach (var instant in instants)
        foreach (var amount in new[] { 1, -1, 7, -7, 25 })
        {
            yield return new object[] { zoneId, instant.Ticks, amount };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AddYears_and_AddMonths_should_match_NodaTime_lenient_calendar_math(string zoneId, long utcTicks, int amount)
    {
        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;
        var calendar = timezone.Clock.Calendar;

        var tzdt = new TimezoneDateTime(new DateTime(utcTicks, DateTimeKind.Utc), timezone);
        var local = Instant.FromUnixTimeTicks(utcTicks - BclUnixEpochTicks).InZone(zone, calendar).LocalDateTime;

        var expectedYears = zone.AtLeniently(local.Date.PlusYears(amount).At(local.TimeOfDay)).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;
        var expectedMonths = zone.AtLeniently(local.Date.PlusMonths(amount).At(local.TimeOfDay)).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;

        tzdt.AddYears(amount).Ticks.Should().Be(expectedYears);
        tzdt.AddMonths(amount).Ticks.Should().Be(expectedMonths);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Month_boundaries_should_match_NodaTime_calendar_math(string zoneId, long utcTicks, int amount)
    {
        _ = amount; // boundaries are amount-independent; reuse the case matrix

        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;
        var calendar = timezone.Clock.Calendar;

        var tzdt = new TimezoneDateTime(new DateTime(utcTicks, DateTimeKind.Utc), timezone);
        var local = Instant.FromUnixTimeTicks(utcTicks - BclUnixEpochTicks).InZone(zone, calendar).LocalDateTime;

        var firstDay = new LocalDate(local.Year, local.Month, 1, local.Calendar);
        var expectedStart = firstDay.AtStartOfDayInZone(zone).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;
        var expectedNext = firstDay.PlusMonths(1).AtStartOfDayInZone(zone).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;

        tzdt.GetStartOfMonth().Ticks.Should().Be(expectedStart);
        tzdt.GetStartOfNextMonth().Ticks.Should().Be(expectedNext);
    }
}
