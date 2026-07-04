namespace R8.TzDateTime.Tests;

/// <summary>
///     Allocation regression guards for the zoned (non-UTC) hot paths. NodaTime's MapLocal allocates a
///     ZoneLocalMapping per resolution; the struct resolves against cached ZoneIntervals instead, so
///     these operations must not allocate per call.
/// </summary>
public class TimezoneDateTimeAllocationTests
{
    private const int Iterations = 10_000;
    private const long MaxAllocatedBytes = 50_000; // JIT/test-host slack, ~5 bytes per op

    private static LocalTimezone Tehran => LocalTimezone.GetTimezone("Asia/Tehran");

    private static long MeasureAllocations(Action action)
    {
        // Warm up so JIT/tiering allocations don't pollute the measurement.
        for (var i = 0; i < 2_000; i++)
            action();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iterations; i++)
            action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void AddDays_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() => _ = tzdt.AddDays(3)).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void AddYears_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() => _ = tzdt.AddYears(1)).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void GetStartOfNextDay_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() => _ = tzdt.GetStartOfNextDay()).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void GetStartOfNextWeek_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() => _ = tzdt.GetStartOfNextWeek()).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void GetStartOfNextMonth_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() => _ = tzdt.GetStartOfNextMonth()).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void Component_constructor_on_zoned_values_should_not_allocate()
    {
        var tehran = Tehran;

        MeasureAllocations(() => _ = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, tehran)).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void Chained_AddYears_across_far_future_years_should_not_allocate()
    {
        // Mirrors the MemoryTests access pattern: each step lands on a new year, walking
        // far past the zone's final DST transition.
        var tzdt_bk = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);
        var tzdt = tzdt_bk;

        MeasureAllocations(() => tzdt = tzdt.Ticks < new DateTime(3000, 1, 1).Ticks ? tzdt.AddYears(1) : tzdt_bk).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void Chained_GetStartOfNextMonth_across_far_future_months_should_not_allocate()
    {
        var tzdt_bk = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);
        var tzdt = tzdt_bk;

        MeasureAllocations(() => tzdt = tzdt.Ticks < new DateTime(3000, 1, 1).Ticks ? tzdt.GetStartOfNextMonth() : tzdt_bk).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void Timezones_property_access_should_not_allocate()
    {
        // The property returns a cached read-only snapshot (rebuilt only at registration), so repeated
        // reads must not allocate. The previous implementation rebuilt an array (+ HashSet) per access.
        _ = Tehran; // ensure at least one non-UTC zone is registered

        MeasureAllocations(() => _ = LocalTimezone.Timezones).Should().BeLessThan(MaxAllocatedBytes);
    }

    [Fact]
    public void Parts_and_GetDateTime_on_zoned_values_should_not_allocate()
    {
        var tzdt = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, Tehran);

        MeasureAllocations(() =>
        {
            _ = tzdt.Year;
            _ = tzdt.Minute;
            _ = tzdt.DayOfWeek;
            _ = tzdt.GetDateTime();
        }).Should().BeLessThan(MaxAllocatedBytes);
    }
}
