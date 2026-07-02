using System.Globalization;
using NodaTime;

namespace R8.TzDateTime;

/// <summary>
///     Describes the identity of a timezone: its IANA ids, culture, ordered days of the week, and clock.
/// </summary>
public interface ITimezoneInfo
{
    /// <summary>
    ///     Gets a collection of IANA timezone identifiers.
    /// </summary>
    string[] IanaIds { get; }

    /// <summary>
    ///     Gets the default IANA timezone identifier.
    /// </summary>
    string DefaultIanaId { get; }

    /// <summary>
    ///     Returns the culture info of the timezone.
    /// </summary>
    CultureInfo Culture { get; }

    /// <summary>
    ///     Gets the ordered days of the week according to the timezone
    /// </summary>
    DayOfWeek[] DaysOfWeek { get; }

    /// <summary>
    ///     Gets the clock instance for the timezone.
    /// </summary>
    ZonedClock Clock { get; }
}