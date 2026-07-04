using NodaTime.TimeZones;
using R8.TzDateTime.TimezoneMappers;

namespace R8.TzDateTime.Tests;

public class TimezoneRegistryTests
{
    [Fact]
    public void Timezone_resolves_by_its_windows_id_as_well_as_its_iana_id()
    {
        var tehran = LocalTimezone.GetTimezone("Asia/Tehran"); // registered by TestTimezones

        // Windows id for Asia/Tehran, from NodaTime's CLDR mapping (e.g. "Iran Standard Time").
        TzdbDateTimeZoneSource.Default.TzdbToWindowsIds.TryGetValue("Asia/Tehran", out var windowsId).Should().BeTrue();

        LocalTimezone.GetTimezone(windowsId!).Should().Be(tehran);
        LocalTimezone.GetTimezone("Iran Standard Time").Should().Be(tehran); // the user-facing Windows id
        LocalTimezone.TryGetTimezone("Iran Standard Time", out var byWindows).Should().BeTrue();
        byWindows!.DefaultIanaId.Should().Be("Asia/Tehran"); // canonical id unchanged

        // The public IanaIds stays the declared IANA set — the Windows id is a resolution alias only.
        tehran.IanaIds.Should().NotContain("Iran Standard Time");
    }


    [Fact]
    public void MappedTimezone_built_from_data_exposes_the_same_metadata_as_a_hand_written_options_class()
    {
        // A timezone defined purely from data — no dedicated LocalTimezoneOptions subclass.
        LocalTimezoneOptions options = new MappedTimezone(
            new[] { "Asia/Tehran", "Iran" },
            CultureInfo.GetCultureInfo("fa-IR"),
            CalendarSystem.PersianSimple);

        options.DefaultIanaId.Should().Be("Asia/Tehran");
        options.IanaIds.Should().Equal("Asia/Tehran", "Iran");
        options.Culture.Name.Should().Be("fa-IR");
        options.Calendar.Should().Be(CalendarSystem.PersianSimple);

        // DaysOfWeek is computed from the culture's first day of week (Saturday for fa-IR).
        options.DaysOfWeek[0].Should().Be(DayOfWeek.Saturday);
        options.DaysOfWeek[6].Should().Be(DayOfWeek.Friday);
    }

    [Theory]
    [InlineData("Asia/Tehran", "Iran", "fa-IR")]
    [InlineData("Asia/Baghdad", "Iraq", "ar-IQ")]
    [InlineData("Europe/Istanbul", "Turkey", "tr-TR")]
    public void registered_timezones_resolve_from_their_canonical_id_and_alias(string canonical, string alias, string culture)
    {
        // Registered by TestTimezones (module initializer) via AddTimezone.
        var byCanonical = LocalTimezone.GetTimezone(canonical);
        var byAlias = LocalTimezone.GetTimezone(alias);

        byCanonical.Should().Be(byAlias);
        byCanonical.DefaultIanaId.Should().Be(canonical);
        byCanonical.IanaIds.Should().Contain(alias);
        byCanonical.Culture.Name.Should().Be(culture);
    }

    [Fact]
    public void Utc_is_the_only_built_in_timezone_and_keeps_index_zero()
    {
        // default(TimezoneDateTime), JSON null, and the UTC fast path all depend on UTC being index 0.
        default(TimezoneDateTime).GetTimezone().DefaultIanaId.Should().Be("UTC");
        LocalTimezone.Utc.DefaultIanaId.Should().Be("UTC");
    }

    [Fact]
    public void AddTimezone_registers_a_zone_resolvable_by_id_alias_and_index()
    {
        var added = LocalTimezone.AddTimezone("America/New_York", CultureInfo.GetCultureInfo("en-US"), CalendarSystem.Gregorian, "US/Eastern");

        LocalTimezone.GetTimezone("America/New_York").Should().Be(added);
        LocalTimezone.GetTimezone("US/Eastern").Should().Be(added); // by alias
        LocalTimezone.TryGetTimezone("America/New_York", out _).Should().BeTrue();

        // Round-trips through the index → timezone table (GetTimezoneByIndex).
        var value = new TimezoneDateTime(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), added);
        value.GetTimezone().DefaultIanaId.Should().Be("America/New_York");

        // Idempotent.
        LocalTimezone.AddTimezone("America/New_York", CultureInfo.GetCultureInfo("en-US"), CalendarSystem.Gregorian).Should().Be(added);
    }

    [Fact]
    public void AddTimezone_is_safe_under_concurrent_add_and_use()
    {
        // Guards the publication ordering: a value built from a freshly-added id must never resolve to an
        // index the lookup table hasn't grown to yet.
        Parallel.For(0, 2000, i =>
        {
            var tz = LocalTimezone.AddTimezone("America/Chicago", CultureInfo.GetCultureInfo("en-US"), CalendarSystem.Gregorian);
            var value = new TimezoneDateTime(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), tz);
            _ = value.Year + value.Hour; // exercises GetTimezoneByIndex on the hot path
        });

        LocalTimezone.GetTimezone("America/Chicago").DefaultIanaId.Should().Be("America/Chicago");
    }
}
