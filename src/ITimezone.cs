using NodaTime;

namespace R8.TzDateTime;

/// <summary>
///     Describes a timezone: its identifiers and culture (via <see cref="ITimezoneInfo" />) plus its
///     calendar system and current UTC offset.
/// </summary>
public interface ITimezone : ITimezoneInfo, IEquatable<ITimezone>
{
    /// <summary>
    ///     Gets the calendar system used by this timezone.
    /// </summary>
    CalendarSystem Calendar { get; }

    /// <summary>
    ///     Gets the offset of this timezone.
    /// </summary>
    Offset Offset { get; }

    /// <summary>
    ///     Returns the system timezone of the current timezone.
    /// </summary>
    TimeZoneInfo GetSystemTimeZone();
}