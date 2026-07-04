namespace R8.TzDateTime.Tests;

/// <summary>
///     Guards for the concurrency contract of LocalTimezone and TimezoneDateTime:
///     scope isolation across async flows, safe first-touch initialization, and
///     consistent reads on shared values under parallel load.
/// </summary>
public class TimezoneConcurrencyTests
{
    // A fixed set (not the live, mutable registry) so other tests registering zones can't perturb this.
    private static readonly string[] ZoneIds = TestTimezones.Ids;

    [Fact]
    public async Task Scopes_should_be_isolated_between_parallel_async_flows()
    {
        var failures = new ConcurrentQueue<string>();

        var tasks = Enumerable.Range(0, 16).Select(i => Task.Run(async () =>
        {
            var zone = LocalTimezone.GetTimezone(ZoneIds[i % ZoneIds.Length]);
            for (var iteration = 0; iteration < 200; iteration++)
            {
                LocalTimezone.StartScope(zone);
                try
                {
                    await Task.Yield();
                    if (LocalTimezone.Current != zone)
                        failures.Enqueue($"task {i}: expected {zone.DefaultIanaId}, got {LocalTimezone.Current.DefaultIanaId}");
                }
                finally
                {
                    LocalTimezone.EndScope();
                }
            }
        }));

        await Task.WhenAll(tasks);

        failures.Should().BeEmpty();
    }

    [Fact]
    public void GetTimezone_should_return_the_same_cached_instance_under_parallel_first_touch()
    {
        foreach (var ianaId in ZoneIds)
        {
            var instances = new ConcurrentBag<LocalTimezone>();

            Parallel.For(0, 64, _ => instances.Add(LocalTimezone.GetTimezone(ianaId)));

            instances.Distinct().Should().HaveCount(1, because: $"'{ianaId}' must resolve to a single cached instance");
        }
    }

    [Fact]
    public void Parallel_reads_on_shared_values_should_be_consistent()
    {
        var tehran = LocalTimezone.GetTimezone("Asia/Tehran");
        var utcValue = new TimezoneDateTime(new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc), LocalTimezone.Utc);
        var tehranValue = new TimezoneDateTime(1402, 10, 25, 13, 50, 30, tehran);

        var expectedUtc = (utcValue.Year, utcValue.Month, utcValue.Day, utcValue.Hour, utcValue.AddDays(1).Ticks, utcValue.ToString("s", null));
        var expectedTehran = (tehranValue.Year, tehranValue.Month, tehranValue.Day, tehranValue.Hour, tehranValue.AddDays(1).Ticks, tehranValue.ToString("s", null));

        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, 20_000, i =>
        {
            var (value, expected) = i % 2 == 0 ? (utcValue, expectedUtc) : (tehranValue, expectedTehran);
            var actual = (value.Year, value.Month, value.Day, value.Hour, value.AddDays(1).Ticks, value.ToString("s", null));
            if (actual != expected)
                failures.Enqueue($"iteration {i}: {actual} != {expected}");
        });

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task Now_should_respect_the_ambient_scope_under_parallelism()
    {
        var failures = new ConcurrentQueue<string>();

        var tasks = Enumerable.Range(0, 16).Select(i => Task.Run(async () =>
        {
            var zone = LocalTimezone.GetTimezone(ZoneIds[i % ZoneIds.Length]);
            for (var iteration = 0; iteration < 100; iteration++)
            {
                LocalTimezone.StartScope(zone);
                try
                {
                    await Task.Yield();
                    var now = TimezoneDateTime.Now;
                    if (!now.GetTimezone().Equals(zone))
                        failures.Enqueue($"task {i}: Now used {now.GetTimezone().DefaultIanaId}, expected {zone.DefaultIanaId}");
                }
                finally
                {
                    LocalTimezone.EndScope();
                }
            }
        }));

        await Task.WhenAll(tasks);

        failures.Should().BeEmpty();
    }
}
