namespace R8.TzDateTime.Tests;

/// <summary>
///     Proves the resolution cores are timezone-agnostic: they are swept against hostile tzdb zones
///     that are NOT registered in LocalTimezone — negative offsets, 30/45-minute offsets and DST
///     shifts, midnight-spanning gaps, a whole skipped calendar day, and perpetual DST — and must
///     match NodaTime's own resolution at every transition. The registered zones are just examples;
///     any future registration must work unchanged.
/// </summary>
public class TimezoneAgnosticismTests
{
    private const long BclUnixEpochTicks = 621355968000000000;

    private static readonly int[] WallSampleMinutes =
    {
        -1500, -121, -61, -60, -59, -30, -1, 0, 1, 30, 59, 60, 61, 121, 700, 1439, 1500,
    };

    public static IEnumerable<object[]> HostileZoneIds()
    {
        yield return new object[] { "America/New_York" }; // negative offset, perpetual DST
        yield return new object[] { "America/St_Johns" }; // -03:30 base offset
        yield return new object[] { "America/Sao_Paulo" }; // midnight spring-forward gaps
        yield return new object[] { "Australia/Lord_Howe" }; // +10:30 base, 30-minute DST shift
        yield return new object[] { "Pacific/Chatham" }; // +12:45 base offset
        yield return new object[] { "Pacific/Apia" }; // skipped an entire calendar day (2011-12-30)
        yield return new object[] { "Europe/London" }; // ongoing DST with no final interval
        yield return new object[] { "Asia/Kathmandu" }; // +05:45, offset changed in 1986
    }

    private static IEnumerable<Instant> GetTransitions(DateTimeZone zone)
    {
        var current = Instant.FromUtc(1950, 1, 1, 0, 0);
        var end = Instant.FromUtc(2030, 1, 1, 0, 0);
        while (current < end)
        {
            var interval = zone.GetZoneInterval(current);
            if (!interval.HasEnd)
                yield break;

            yield return interval.End;
            current = interval.End;
        }
    }

    [Theory]
    [MemberData(nameof(HostileZoneIds))]
    public void Lenient_resolution_core_should_match_NodaTime_for_unregistered_zones(string zoneId)
    {
        var zone = DateTimeZoneProviders.Tzdb[zoneId];

        var checkedSamples = 0;
        foreach (var transition in GetTransitions(zone))
        {
            var wallOffsetBefore = zone.GetZoneInterval(transition.Minus(Duration.Epsilon)).WallOffset;
            var transitionWallTicks = transition.ToUnixTimeTicks() + wallOffsetBefore.Ticks + BclUnixEpochTicks;

            foreach (var offsetMinutes in WallSampleMinutes)
            {
                var wallTicks = transitionWallTicks + offsetMinutes * TimeSpan.TicksPerMinute;
                if ((ulong)wallTicks > (ulong)DateTime.MaxValue.Ticks)
                    continue;

                var local = LocalDateTime.FromDateTime(new DateTime(wallTicks));
                var expected = zone.AtLeniently(local).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;

                var actual = TimezoneDateTime.ResolveLenientTicksCore(wallTicks, zone);

                actual.Should().Be(expected, because: $"wall {new DateTime(wallTicks):O} near transition {transition} in {zoneId} must resolve like AtLeniently");
                checkedSamples++;
            }
        }

        checkedSamples.Should().BeGreaterThan(0, because: $"{zoneId} must have transitions in the sweep window");
    }

    [Theory]
    [MemberData(nameof(HostileZoneIds))]
    public void Start_of_day_resolution_core_should_match_NodaTime_for_unregistered_zones(string zoneId)
    {
        var zone = DateTimeZoneProviders.Tzdb[zoneId];

        var checkedDays = 0;
        foreach (var transition in GetTransitions(zone))
        {
            // The days around each transition, including any day whose midnight is skipped.
            for (var dayOffset = -2; dayOffset <= 2; dayOffset++)
            {
                var probeInstant = transition.Plus(Duration.FromDays(dayOffset)).Plus(Duration.FromHours(12));
                var date = probeInstant.InZone(zone).Date;

                var expected = date.AtStartOfDayInZone(zone).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;

                var wallMidnightTicks = date.AtMidnight().ToDateTimeUnspecified().Ticks;
                var actual = TimezoneDateTime.ResolveStartOfDayTicksCore(wallMidnightTicks, zone);

                actual.Should().Be(expected, because: $"start of {date} in {zoneId} must match AtStartOfDayInZone");
                checkedDays++;
            }
        }

        checkedDays.Should().BeGreaterThan(0, because: $"{zoneId} must have transitions in the sweep window");
    }
}
