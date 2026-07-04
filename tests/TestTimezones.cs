using System.Runtime.CompilerServices;

namespace R8.TzDateTime.Tests;

/// <summary>
///     Registers the timezones the test suite relies on. Runs once, before any test, since built-in
///     timezones (other than UTC) were removed in favor of runtime registration via
///     <see cref="LocalTimezone.AddTimezone(string, System.Globalization.CultureInfo, NodaTime.CalendarSystem, string[])" />.
/// </summary>
internal static class TestTimezones
{
    /// <summary>
    ///     The fixed set of canonical ids registered here. Data-driven tests enumerate THIS rather than the
    ///     live <see cref="LocalTimezone.Timezones" />, so other tests that register throwaway zones at
    ///     runtime cannot perturb their case count (the registry is process-global and mutable).
    /// </summary>
    internal static readonly string[] Ids = { "UTC", "Asia/Tehran", "Asia/Baghdad", "Europe/Istanbul" };

    [ModuleInitializer]
    internal static void Register()
    {
        LocalTimezone.AddTimezone("Asia/Tehran", CultureInfo.GetCultureInfo("fa-IR"), CalendarSystem.PersianSimple, "Iran");
        LocalTimezone.AddTimezone("Asia/Baghdad", CultureInfo.GetCultureInfo("ar-IQ"), CalendarSystem.Gregorian, "Iraq");
        LocalTimezone.AddTimezone("Europe/Istanbul", CultureInfo.GetCultureInfo("tr-TR"), CalendarSystem.Gregorian, "Turkey");
    }
}
