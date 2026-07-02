using System.Globalization;
using NodaTime;
using R8.TzDateTime;

// Runs in a Native-AOT-published binary. Exit code 0 = all good, non-zero = a regression.
var failures = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}: {name}");
    if (!ok) failures++;
}

// Register a Persian-calendar zone at runtime (the API surface consumers use).
var tehran = LocalTimezone.AddTimezone("Asia/Tehran", CultureInfo.GetCultureInfo("fa-IR"), CalendarSystem.PersianSimple, "Iran");
Check("Tehran culture is fa-IR", tehran.Culture.Name == "fa-IR");
Check("Tehran uses the Persian calendar", tehran.Calendar == CalendarSystem.PersianSimple);
Check("Tehran is RTL (globalization survived AOT)", tehran.IsRTL);
Check("Tehran week starts on Saturday", tehran.DaysOfWeek[0] == DayOfWeek.Saturday);
Check("Tehran resolves via its 'Iran' alias", LocalTimezone.GetTimezone("Iran") == tehran);

// A Gregorian instant read back in the Persian calendar.
var value = new TimezoneDateTime(new DateTime(2024, 3, 20, 20, 30, 0, DateTimeKind.Utc), tehran);
Check($"Persian year is 1403 (got {value.Year})", value.Year == 1403);
Check("Add/format work", value.AddDays(1).ToString().Length > 0);

Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURE(S)");
return failures;
