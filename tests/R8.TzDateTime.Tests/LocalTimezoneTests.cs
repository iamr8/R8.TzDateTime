namespace R8.TzDateTime.Tests;

public class LocalTimezoneTests
{
    [Fact]
    public void NOT_TEST_only_for_debugging_current_Timezone_when_is_not_set()
    {
        var tzdt = LocalTimezone.Current;
        tzdt.Should().NotBeNull();
    }

    [Fact]
    public void GetTimezones_should_enumerate_without_throwing_and_return_only_configured_timezones()
    {
        var act = () => LocalTimezone.GetTimezones().ToArray();

        act.Should().NotThrow();
        var timezones = act();
        timezones.Should().NotBeEmpty();
        timezones.All(timezone => LocalTimezone.TryGetTimezone(timezone.DefaultIanaId, out _)).Should().BeTrue();
        timezones.Select(timezone => timezone.DefaultIanaId).Should().Contain("Asia/Tehran");
    }

    [Fact]
    public void GetTimezonesGroupedByOffset_should_not_throw_for_the_unmapped_timezones_path()
    {
        var act = () => LocalTimezone.GetTimezonesGroupedByOffset(onlyMappedTimezones: false);

        act.Should().NotThrow();
        var groups = act();
        groups.Should().NotBeEmpty();
        groups.SelectMany(group => group.IanaIds.Keys).Should().Contain("Asia/Tehran");
    }

    [Fact]
    public void GetTimezonesGroupedByOffset_should_group_mapped_timezones_by_offset()
    {
        var groups = LocalTimezone.GetTimezonesGroupedByOffset(onlyMappedTimezones: true);

        groups.Should().NotBeEmpty();
        groups.SelectMany(group => group.IanaIds.Keys).Should().Contain(new[] { "Asia/Tehran", "Asia/Baghdad", "Europe/Istanbul" });
    }
}