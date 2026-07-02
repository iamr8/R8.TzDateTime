namespace R8.TzDateTime.Tests.Timezones;

public class LosAngelesTimezone : LocalTimezoneOptions
{
    public override string[] IanaIds => new[] { "America/Los_Angeles" };
    public override CultureInfo Culture => CultureInfo.GetCultureInfo("en-US");
    public override CalendarSystem Calendar => CalendarSystem.Gregorian;
}