namespace R8.TzDateTime.Tests;

/// <summary>
///     Pins TimezoneDateTime's local-to-instant resolution to NodaTime's semantics across every
///     tzdb transition of the supported zones: constructor and AddDays must match AtLeniently
///     (ambiguous → earlier offset, skipped → shifted forward by the gap), and GetStartOfDay must
///     match LocalDate.AtStartOfDayInZone (skipped midnight → first valid instant of the day).
///     These tests guard any internal reimplementation of the resolution path.
/// </summary>
public class TimezoneResolverEquivalenceTests
{
    private const long BclUnixEpochTicks = 621355968000000000;

    private static readonly (int OffsetMinutes, string Description)[] WallSampleOffsets =
    {
        (-121, "well before"), (-90, "before"), (-61, "just before ambiguity"), (-60, "ambiguity edge"),
        (-59, "inside earlier window"), (-30, "inside window"), (-1, "last minute"),
        (0, "transition wall point"), (1, "first minute"), (30, "inside window"),
        (59, "inside later window"), (60, "gap edge"), (61, "just after"), (90, "after"), (121, "well after"),
    };

    public static IEnumerable<object[]> SupportedZoneIds()
    {
        // A fixed set (not the live, mutable registry) so other tests registering zones can't change the
        // case count. Only zones that have transitions to sweep are yielded.
        foreach (var zoneId in TestTimezones.Ids)
        {
            var timezone = LocalTimezone.GetTimezone(zoneId);
            if (timezone.Clock.Zone.GetZoneInterval(Instant.FromUtc(1970, 1, 1, 0, 0)).HasEnd)
                yield return new object[] { zoneId };
        }
    }

    private static IEnumerable<Instant> GetTransitions(DateTimeZone zone)
    {
        var current = Instant.FromUtc(1970, 1, 1, 0, 0);
        var end = Instant.FromUtc(2025, 1, 1, 0, 0);
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
    [MemberData(nameof(SupportedZoneIds))]
    public void Constructor_should_match_NodaTime_lenient_resolution_around_every_transition(string zoneId)
    {
        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;
        var calendar = timezone.Clock.Calendar;

        var checkedSamples = 0;
        foreach (var transition in GetTransitions(zone))
        {
            var wallOffsetBefore = zone.GetZoneInterval(transition.Minus(Duration.Epsilon)).WallOffset;
            var transitionWallTicks = transition.ToUnixTimeTicks() + wallOffsetBefore.Ticks + BclUnixEpochTicks;

            foreach (var (offsetMinutes, description) in WallSampleOffsets)
            {
                var wallDateTime = new DateTime(transitionWallTicks, DateTimeKind.Unspecified).AddMinutes(offsetMinutes);
                var local = LocalDateTime.FromDateTime(wallDateTime, calendar);

                var expectedUtcTicks = zone.AtLeniently(local).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;
                var actual = new TimezoneDateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second, local.Millisecond, timezone);

                actual.Ticks.Should().Be(expectedUtcTicks, because: $"wall {wallDateTime:O} ({description}) near transition {transition} in {zoneId} must resolve leniently");
                checkedSamples++;
            }
        }

        checkedSamples.Should().BeGreaterThan(0, because: $"{zoneId} must have transitions in the sweep window");
    }

    [Theory]
    [MemberData(nameof(SupportedZoneIds))]
    public void AddDays_should_match_NodaTime_lenient_resolution_around_every_transition(string zoneId)
    {
        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;
        var calendar = timezone.Clock.Calendar;

        foreach (var transition in GetTransitions(zone))
        {
            var wallOffsetBefore = zone.GetZoneInterval(transition.Minus(Duration.Epsilon)).WallOffset;
            var transitionWallTicks = transition.ToUnixTimeTicks() + wallOffsetBefore.Ticks + BclUnixEpochTicks;

            foreach (var (offsetMinutes, description) in WallSampleOffsets)
            {
                // Start one day before the sampled wall time, then step one day forward onto it.
                var targetWall = new DateTime(transitionWallTicks, DateTimeKind.Unspecified).AddMinutes(offsetMinutes);
                var baseWall = targetWall.AddDays(-1);
                var baseLocal = LocalDateTime.FromDateTime(baseWall, calendar);

                var start = new TimezoneDateTime(new DateTime(zone.AtLeniently(baseLocal).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks, DateTimeKind.Utc), timezone);
                var expectedLocal = zone.AtLeniently(baseLocal.Date.PlusDays(1).At(baseLocal.TimeOfDay));
                var expectedUtcTicks = expectedLocal.ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;

                start.AddDays(1).Ticks.Should().Be(expectedUtcTicks, because: $"stepping onto wall {targetWall:O} ({description}) near {transition} in {zoneId} must resolve leniently");
            }
        }
    }

    [Theory]
    [MemberData(nameof(SupportedZoneIds))]
    public void GetStartOfDay_should_match_NodaTime_AtStartOfDayInZone_around_every_transition(string zoneId)
    {
        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;
        var calendar = timezone.Clock.Calendar;

        foreach (var transition in GetTransitions(zone))
        {
            // Check the day containing the transition plus its neighbors.
            for (var dayOffset = -1; dayOffset <= 1; dayOffset++)
            {
                var instant = transition.Plus(Duration.FromDays(dayOffset)).Plus(Duration.FromHours(6));
                var date = instant.InZone(zone, calendar).Date;

                var expectedUtcTicks = date.AtStartOfDayInZone(zone).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks;
                var tzdt = new TimezoneDateTime(new DateTime(instant.ToUnixTimeTicks() + BclUnixEpochTicks, DateTimeKind.Utc), timezone);

                tzdt.GetStartOfDay().Ticks.Should().Be(expectedUtcTicks, because: $"start of {date} in {zoneId} must match AtStartOfDayInZone");
                tzdt.GetStartOfNextDay().Ticks.Should().Be(date.PlusDays(1).AtStartOfDayInZone(zone).ToInstant().ToUnixTimeTicks() + BclUnixEpochTicks);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SupportedZoneIds))]
    public void Boundary_operations_should_stay_consistent_around_every_transition(string zoneId)
    {
        var timezone = LocalTimezone.GetTimezone(zoneId);
        var zone = timezone.Clock.Zone;

        foreach (var transition in GetTransitions(zone))
        {
            foreach (var hourOffset in new[] { -25, -3, -1, 0, 1, 3, 25 })
            {
                var instant = transition.Plus(Duration.FromHours(hourOffset)).Plus(Duration.FromMinutes(17));
                var tzdt = new TimezoneDateTime(new DateTime(instant.ToUnixTimeTicks() + BclUnixEpochTicks, DateTimeKind.Utc), timezone);

                tzdt.GetEndOfHour().Ticks.Should().Be(tzdt.GetStartOfNextHour().Ticks - 1);
                tzdt.GetEndOfDay().Ticks.Should().Be(tzdt.GetStartOfNextDay().Ticks - 1);
                tzdt.GetEndOfWeek().Ticks.Should().Be(tzdt.GetStartOfNextWeek().Ticks - 1);
                tzdt.GetEndOfMonth().Ticks.Should().Be(tzdt.GetStartOfNextMonth().Ticks - 1);
                tzdt.GetStartOfHour().Ticks.Should().BeLessThanOrEqualTo(tzdt.Ticks);
                tzdt.GetStartOfDay().Ticks.Should().BeLessThanOrEqualTo(tzdt.Ticks);
            }
        }
    }
}
