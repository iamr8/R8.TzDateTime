namespace R8.TzDateTime.Tests;

public class MemoryTests
{
    private const int WarmupIterations = 10_000;

    private readonly ITestOutputHelper _outputHelper;

    public MemoryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    /// <summary>
    ///     Runs the loop once for JIT warmup, then measures wall time and per-thread allocated bytes.
    ///     The returned checksum is printed so the JIT cannot eliminate the measured work.
    /// </summary>
    private void Measure(string label, int iterations, Func<int, long> loop)
    {
        _ = loop(WarmupIterations);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopWatch = Stopwatch.StartNew();
        var checksum = loop(iterations);
        stopWatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        _outputHelper.WriteLine($"{label}: {stopWatch.Elapsed.TotalMilliseconds:F1} ms, {allocated} bytes allocated (checksum {checksum})");
    }

    [Fact]
    public void SizeOf()
    {
        unsafe
        {
            var dtSize = sizeof(DateTime);
            _outputHelper.WriteLine($"DateTime size: {dtSize}");
            var tzdtSize = sizeof(TimezoneDateTime);
            _outputHelper.WriteLine($"TimezoneDateTime size: {tzdtSize}");
            var localDate = sizeof(LocalDateTime);
            _outputHelper.WriteLine($"LocalDateTime size: {localDate}");
#pragma warning disable CS8500 // ZonedDateTime is a managed type; sizeof is used here only for diagnostic output.
            var localDate2 = sizeof(ZonedDateTime);
#pragma warning restore CS8500
            _outputHelper.WriteLine($"ZonedDateTime size: {localDate2}");

            tzdtSize.Should().Be(16);
        }
    }

    [Theory]
    [InlineData(1_000_000)]
    public void AddYears_AddMinutes_AddSeconds_UTC(int iterations)
    {
        var dt_bk = new DateTime(2023, 10, 1);
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc);

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime AddYearsAddMinutesAddSeconds", iterations, n =>
        {
            var dt = dt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    dt = dt_bk;

                dt = dt.AddYears(1).AddMinutes(30).AddSeconds(39);
                checksum += dt.Ticks;
            }

            return checksum;
        });

        Measure("TimezoneDateTime AddYearsAddMinutesAddSeconds", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.AddYears(1).AddMinutes(30).AddSeconds(39);
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void AddYears_AddMinutes_AddSeconds_Tehran(int iterations)
    {
        var dt_bk = new DateTime(2023, 10, 1);
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime AddYearsAddMinutesAddSeconds", iterations, n =>
        {
            var dt = dt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    dt = dt_bk;

                dt = dt.AddYears(1).AddMinutes(30).AddSeconds(39);
                checksum += dt.Ticks;
            }

            return checksum;
        });

        Measure("TimezoneDateTime AddYearsAddMinutesAddSeconds", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.AddYears(1).AddMinutes(30).AddSeconds(39);
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void AddYears_UTC(int iterations)
    {
        var dt_bk = new DateTime(2023, 10, 1);
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc);

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime AddYears", iterations, n =>
        {
            var dt = dt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    dt = dt_bk;

                dt = dt.AddYears(1);
                checksum += dt.Ticks;
            }

            return checksum;
        });

        Measure("TimezoneDateTime AddYears", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.AddYears(1);
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void AddYears_Tehran(int iterations)
    {
        var dt_bk = new DateTime(2023, 10, 1);
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime AddYears", iterations, n =>
        {
            var dt = dt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    dt = dt_bk;

                dt = dt.AddYears(1);
                checksum += dt.Ticks;
            }

            return checksum;
        });

        Measure("TimezoneDateTime AddYears", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.AddYears(1);
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetYear_UTC(int iterations)
    {
        var dt = new DateTime(2023, 10, 1);
        var tzdt = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc);

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime GetYear", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += dt.Year;

            return checksum;
        });

        Measure("TimezoneDateTime GetYear", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += tzdt.Year;

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetYear_Tehran(int iterations)
    {
        var dt = new DateTime(2023, 10, 1);
        var tzdt = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime GetYear", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += dt.Year;

            return checksum;
        });

        Measure("TimezoneDateTime GetYear", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += tzdt.Year;

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetMinute_UTC(int iterations)
    {
        var dt = new DateTime(2023, 10, 1);
        var tzdt = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc);

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime GetMinute", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += dt.Minute;

            return checksum;
        });

        Measure("TimezoneDateTime GetMinute", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += tzdt.Minute;

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetMinute_Tehran(int iterations)
    {
        var dt = new DateTime(2023, 10, 1);
        var tzdt = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("DateTime GetMinute", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += dt.Minute;

            return checksum;
        });

        Measure("TimezoneDateTime GetMinute", iterations, n =>
        {
            long checksum = 0;
            for (var i = 0; i < n; i++)
                checksum += tzdt.Minute;

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetStartOfNextWeek_Tehran(int iterations)
    {
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("TimezoneDateTime GetStartOfNextWeek", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.GetStartOfNextWeek();
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetStartOfNextMonth_Tehran(int iterations)
    {
        var tzdt_bk = new TimezoneDateTime(1403, 10, 05, LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("TimezoneDateTime GetStartOfNextMonth", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.GetStartOfNextMonth();
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetStartOfNextMonth_LastMonthOfYear_Tehran(int iterations)
    {
        var tzdt_bk = new TimezoneDateTime(1403, 12, 05, LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("TimezoneDateTime GetStartOfNextMonth", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.GetStartOfNextMonth();
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetStartOfWeek_Tehran(int iterations)
    {
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("TimezoneDateTime GetStartOfWeek", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.GetStartOfWeek();
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }

    [Theory]
    [InlineData(1_000_000)]
    public void GetStartOfNextDay_Tehran(int iterations)
    {
        var tzdt_bk = new TimezoneDateTime(2023, 10, 1, LocalTimezone.Utc).WithTimezone(LocalTimezone.GetTimezone("Asia/Tehran"));

        _outputHelper.WriteLine($"Iterations: {iterations}");

        Measure("TimezoneDateTime GetStartOfNextDay", iterations, n =>
        {
            var tzdt = tzdt_bk;
            long checksum = 0;
            for (var i = 0; i < n; i++)
            {
                if (i % 1000 == 0)
                    tzdt = tzdt_bk;

                tzdt = tzdt.GetStartOfNextDay();
                checksum += tzdt.Ticks;
            }

            return checksum;
        });

        Assert.True(true);
    }
}