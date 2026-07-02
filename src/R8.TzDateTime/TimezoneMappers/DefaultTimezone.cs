using System.Globalization;
using NodaTime;

namespace R8.TzDateTime.TimezoneMappers;

/// <summary>
///     Timezone options built from an arbitrary <see cref="DateTimeZone" /> and culture, using the ISO
///     calendar. Use this to configure a timezone that has no dedicated <see cref="LocalTimezoneOptions" />
///     subclass.
/// </summary>
public class DefaultTimezone : LocalTimezoneOptions
{
    /// <summary>
    ///     Initializes a new instance of <see cref="DefaultTimezone" />.
    /// </summary>
    /// <param name="zone">The NodaTime zone whose id becomes the sole IANA id of this timezone.</param>
    /// <param name="culture">The culture associated with this timezone.</param>
    public DefaultTimezone(DateTimeZone zone, CultureInfo culture)
    {
        IanaIds = new[] { zone.Id };
        Culture = culture;
        Calendar = CalendarSystem.Iso;
    }

    /// <inheritdoc />
    public override string[] IanaIds { get; }

    /// <inheritdoc />
    public override CultureInfo Culture { get; }

    /// <inheritdoc />
    public override CalendarSystem Calendar { get; }
}