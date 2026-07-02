namespace R8.TzDateTime.Tests.Timezones;

public class UKTimezone : LocalTimezoneOptions
{
    public override string[] IanaIds => new[] { "Europe/London" };
    public override CultureInfo Culture => CultureInfo.GetCultureInfo("en-GB");
    public override CalendarSystem Calendar => CalendarSystem.Gregorian;
}