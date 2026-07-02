using System.Runtime.CompilerServices;
using NodaTime;

namespace R8.TzDateTime;

/// <summary>
///     Extension methods for converting values between timezones.
/// </summary>
public static class TimezoneExtensions
{
    /// <summary>
    ///     Converts Time of Day to a specific timezone.
    /// </summary>
    /// <param name="timeOfDay">A <see cref="TimeSpan" /> representing a wall-clock time of day (0 through 24 hours).</param>
    /// <param name="sourceTimezone">A Timezone to convert from.</param>
    /// <param name="targetTimezone">A Timezone to convert to.</param>
    /// <returns>The wall-clock time of day in the target timezone for the same instant, based on today's date.</returns>
    public static TimeSpan WithTimezone(this TimeSpan timeOfDay, LocalTimezone sourceTimezone, LocalTimezone targetTimezone)
    {
        if (sourceTimezone == targetTimezone)
            return timeOfDay;

        var today = LocalDate.FromDateTime(DateTime.UtcNow.Date);
        var local = today.At(LocalTime.FromTicksSinceMidnight(timeOfDay.Ticks));
        var instant = local.InZoneLeniently(sourceTimezone.Clock.Zone).ToInstant();
        var target = instant.InZone(targetTimezone.Clock.Zone).LocalDateTime;
        return TimeSpan.FromTicks(target.TimeOfDay.TickOfDay);
    }

    /// <summary>
    ///     Returns a <see cref="TimezoneDateTime" /> from the specified <see cref="DateTime" /> according to the specified
    ///     <see cref="LocalTimezone" />.
    /// </summary>
    /// <param name="dateTime">A <see cref="DateTime" /> object.</param>
    /// <param name="timeZone">A <see cref="LocalTimezone" /> object.</param>
    /// <returns>A <see cref="TimezoneDateTime" /> object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimezoneDateTime ToTimezoneDateTime(this DateTime dateTime, LocalTimezone timeZone)
    {
        if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(dateTime), "DateTime cannot be MinValue or MaxValue.");

        return new TimezoneDateTime(dateTime, timeZone);
    }
}