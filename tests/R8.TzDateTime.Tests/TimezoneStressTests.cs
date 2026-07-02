namespace R8.TzDateTime.Tests;

/// <summary>
///     Thread-based stress and load tests. Expected results are computed once, single-threaded; then
///     dedicated threads recompute them under aggressive contention (thundering herds, global-state
///     writers, forced GC) and every result must match bit-for-bit. Any torn read, cache corruption or
///     race in LocalTimezone/TimezoneDateTime surfaces as a snapshot mismatch or an exception.
/// </summary>
public class TimezoneStressTests
{
    // All registered timezones — new registrations are stressed automatically.
    private static readonly string[] ZoneIds = LocalTimezone.Timezones
        .Select(t => t.DefaultIanaId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

    private static readonly DateTime FixedComparand = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     Instants chosen to exercise both resolver paths: modern dates (final-interval fast path),
    ///     DST gap/ambiguity days of 2022 Tehran, historical DST eras, and far past/future.
    /// </summary>
    private static readonly DateTime[] SampleInstants =
    {
        new(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc),
        new(2026, 7, 1, 23, 59, 59, DateTimeKind.Utc),
        new(2022, 3, 21, 21, 30, 0, DateTimeKind.Utc), // inside Tehran's 2022 spring-forward day
        new(2022, 9, 21, 19, 30, 0, DateTimeKind.Utc), // inside Tehran's 2022 fall-back ambiguity
        new(1995, 6, 10, 8, 0, 0, DateTimeKind.Utc), // historical DST era (Iran, Iraq, Turkey)
        new(1982, 12, 31, 20, 0, 0, DateTimeKind.Utc),
        new(2015, 3, 29, 0, 30, 0, DateTimeKind.Utc), // Istanbul spring-forward
        new(2100, 5, 5, 5, 5, 5, DateTimeKind.Utc), // far future, past every transition
    };

    private static List<TimezoneDateTime> BuildMatrix()
    {
        var matrix = new List<TimezoneDateTime>();
        foreach (var zoneId in ZoneIds)
        {
            var timezone = LocalTimezone.GetTimezone(zoneId);
            foreach (var instant in SampleInstants)
                matrix.Add(new TimezoneDateTime(instant, timezone));
        }

        return matrix;
    }

    /// <summary>
    ///     Computes a full observable snapshot of a value: parts, boundaries, arithmetic, formatting,
    ///     humanization and a JSON round-trip, folded into one comparable string.
    /// </summary>
    private static string Snapshot(TimezoneDateTime value)
    {
        var json = JsonSerializer.Serialize(value);
        var roundTripped = JsonSerializer.Deserialize<TimezoneDateTime>(json);

        return string.Join('|',
            value.Ticks,
            value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond,
            (int)value.DayOfWeek,
            value.GetDaysInMonth(),
            value.IsDaylightSavingTime(),
            value.GetStartOfHour().Ticks, value.GetEndOfHour().Ticks,
            value.GetStartOfDay().Ticks, value.GetEndOfDay().Ticks, value.GetStartOfNextDay().Ticks,
            value.GetStartOfWeek().Ticks, value.GetEndOfWeek().Ticks, value.GetStartOfNextWeek().Ticks,
            value.GetStartOfMonth().Ticks, value.GetEndOfMonth().Ticks, value.GetStartOfNextMonth().Ticks,
            value.AddDays(1).Ticks, value.AddDays(-1).Ticks,
            value.AddMonths(1).Ticks, value.AddYears(1).Ticks, value.AddHours(5).Ticks,
            value.WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran")).Year,
            value.GetDateTime().Ticks,
            value.ToString("s", null),
            value.Humanize(FixedComparand, maxRelativity: null),
            json,
            roundTripped.Ticks);
    }

    /// <summary>
    ///     Starts <paramref name="threadCount" /> dedicated (non-pool) threads, waits for all of them,
    ///     and fails with the collected diagnostics if any thread reported a problem.
    /// </summary>
    private static void RunThreads(int threadCount, Action<int, ConcurrentQueue<string>> body)
    {
        var failures = new ConcurrentQueue<string>();
        var threads = new List<Thread>(threadCount);
        for (var i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            var thread = new Thread(() =>
            {
                try
                {
                    body(threadIndex, failures);
                }
                catch (Exception exception)
                {
                    failures.Enqueue($"thread {threadIndex} crashed: {exception}");
                }
            })
            {
                IsBackground = true,
                Name = $"tz-stress-{i}",
            };
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        failures.Should().BeEmpty();
    }

    [Fact]
    public void Hammering_all_operations_from_dedicated_threads_should_yield_identical_results()
    {
        var matrix = BuildMatrix();
        var expected = matrix.Select(Snapshot).ToArray();

        const int threadCount = 16;
        const int iterationsPerThread = 300;
        var totalComparisons = 0L;

        RunThreads(threadCount, (threadIndex, failures) =>
        {
            var random = new Random(threadIndex * 7919 + 13);
            for (var iteration = 0; iteration < iterationsPerThread; iteration++)
            {
                // Random traversal order per iteration keeps threads hitting different zones at once.
                foreach (var sampleIndex in Enumerable.Range(0, matrix.Count).OrderBy(_ => random.Next()))
                {
                    var actual = Snapshot(matrix[sampleIndex]);
                    if (actual != expected[sampleIndex])
                    {
                        failures.Enqueue($"thread {threadIndex}, sample {sampleIndex}:\nexpected {expected[sampleIndex]}\nactual   {actual}");
                        return;
                    }

                    Interlocked.Increment(ref totalComparisons);
                }
            }
        });

        totalComparisons.Should().Be(threadCount * iterationsPerThread * matrix.Count);
    }

    [Fact]
    public void Thundering_herd_on_a_shared_value_should_stay_consistent()
    {
        var tehran = LocalTimezone.GetTimezone("Asia/Tehran");
        var shared = new TimezoneDateTime(new DateTime(2022, 3, 21, 21, 30, 0, DateTimeKind.Utc), tehran);
        var expected = Snapshot(shared);

        const int threadCount = 32;
        const int rounds = 400;
        using var barrier = new Barrier(threadCount);

        RunThreads(threadCount, (threadIndex, failures) =>
        {
            for (var round = 0; round < rounds; round++)
            {
                // All threads hit the same value at the same moment for maximum contention.
                barrier.SignalAndWait();

                var actual = Snapshot(shared);
                if (actual != expected)
                {
                    failures.Enqueue($"thread {threadIndex}, round {round}: snapshot diverged");
                    return;
                }
            }
        });
    }

    [Fact]
    public async Task Massive_async_scope_churn_should_never_leak_between_flows()
    {
        var failures = new ConcurrentQueue<string>();

        var tasks = Enumerable.Range(0, 128).Select(flowIndex => Task.Run(async () =>
        {
            var zone = LocalTimezone.GetTimezone(ZoneIds[flowIndex % ZoneIds.Length]);
            for (var iteration = 0; iteration < 250; iteration++)
            {
                LocalTimezone.StartScope(zone);
                try
                {
                    if (iteration % 3 == 0)
                        await Task.Yield();
                    if (iteration % 17 == 0)
                        await Task.Delay(1);

                    if (!ReferenceEquals(LocalTimezone.Current, zone))
                    {
                        failures.Enqueue($"flow {flowIndex}: scope leaked, got {LocalTimezone.Current.DefaultIanaId}");
                        return;
                    }

                    var now = TimezoneDateTime.Now;
                    if (!now.GetTimezone().Equals(zone))
                    {
                        failures.Enqueue($"flow {flowIndex}: Now used {now.GetTimezone().DefaultIanaId}");
                        return;
                    }
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
    public void Concurrent_global_current_writers_and_readers_should_never_observe_torn_state()
    {
        var original = LocalTimezone.Current;
        var zones = ZoneIds.Select(LocalTimezone.GetTimezone).ToArray();
        var validIds = ZoneIds.ToHashSet(StringComparer.Ordinal);
        var stop = false;

        try
        {
            RunThreads(24, (threadIndex, failures) =>
            {
                if (threadIndex < 4)
                {
                    // Writers: rotate the process-wide default timezone.
                    for (var i = 0; i < 10_000 && !Volatile.Read(ref stop); i++)
                        LocalTimezone.Current = zones[(threadIndex + i) % zones.Length];
                    return;
                }

                // Readers: the ambient default must always be a fully-initialized, registered timezone.
                for (var i = 0; i < 20_000; i++)
                {
                    var current = LocalTimezone.Current;
                    if (current is null || !validIds.Contains(current.DefaultIanaId))
                    {
                        failures.Enqueue($"reader {threadIndex}: torn or unknown Current");
                        Volatile.Write(ref stop, true);
                        return;
                    }

                    var now = TimezoneDateTime.Now;
                    if (now.Ticks == 0)
                    {
                        failures.Enqueue($"reader {threadIndex}: Now produced an empty value");
                        Volatile.Write(ref stop, true);
                        return;
                    }
                }

                Volatile.Write(ref stop, true);
            });
        }
        finally
        {
            LocalTimezone.Current = original;
        }
    }

    [Fact]
    public void Sustained_load_under_forced_gc_pressure_should_stay_correct()
    {
        var matrix = BuildMatrix();
        var expected = matrix.Select(Snapshot).ToArray();
        var stop = false;

        var gcThread = new Thread(() =>
        {
            while (!Volatile.Read(ref stop))
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                Thread.Sleep(5);
            }
        })
        {
            IsBackground = true,
            Name = "tz-stress-gc",
        };
        gcThread.Start();

        try
        {
            RunThreads(8, (threadIndex, failures) =>
            {
                for (var iteration = 0; iteration < 150; iteration++)
                {
                    for (var sampleIndex = 0; sampleIndex < matrix.Count; sampleIndex++)
                    {
                        var actual = Snapshot(matrix[sampleIndex]);
                        if (actual != expected[sampleIndex])
                        {
                            failures.Enqueue($"thread {threadIndex}, sample {sampleIndex}: diverged under GC pressure");
                            return;
                        }
                    }
                }
            });
        }
        finally
        {
            Volatile.Write(ref stop, true);
            gcThread.Join();
        }
    }

    [Fact]
    public void Parallel_json_round_trips_should_be_isolated_and_lossless()
    {
        var utc = LocalTimezone.Utc;
        var baseInstant = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        RunThreads(16, (threadIndex, failures) =>
        {
            for (var i = 0; i < 12_000; i++)
            {
                // Distinct value per thread and iteration, including sub-second precision.
                var value = new TimezoneDateTime(baseInstant.AddSeconds(threadIndex * 100_000 + i).AddMilliseconds(i % 1000), utc);

                var roundTripped = JsonSerializer.Deserialize<TimezoneDateTime>(JsonSerializer.Serialize(value));
                if (roundTripped != value)
                {
                    failures.Enqueue($"thread {threadIndex}, iteration {i}: {value.Ticks} != {roundTripped.Ticks}");
                    return;
                }
            }
        });
    }
}
