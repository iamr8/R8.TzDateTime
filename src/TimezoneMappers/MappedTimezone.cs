using System.Globalization;
using NodaTime;

namespace R8.TzDateTime.TimezoneMappers;

/// <summary>
///     A <see cref="LocalTimezoneOptions" /> built from explicit data (IANA ids, culture, calendar) rather
///     than a dedicated subclass. All built-in timezones except <see cref="UtcTimezone" /> are registered
///     this way from a data table, so adding one is a single row instead of a new class.
/// </summary>
internal sealed class MappedTimezone : LocalTimezoneOptions
{
    private readonly CalendarSystem _calendar;
    private readonly CultureInfo _culture;
    private readonly string[] _ianaIds;

    /// <summary>
    ///     Initializes a new instance of <see cref="MappedTimezone" />.
    /// </summary>
    /// <param name="ianaIds">The IANA ids this timezone answers to, canonical id first.</param>
    /// <param name="culture">The culture associated with the timezone.</param>
    /// <param name="calendar">The calendar system of the timezone.</param>
    public MappedTimezone(string[] ianaIds, CultureInfo culture, CalendarSystem calendar)
    {
        _ianaIds = ianaIds;
        _culture = culture;
        _calendar = calendar;
    }

    /// <inheritdoc />
    public override string[] IanaIds => _ianaIds;

    /// <inheritdoc />
    public override CultureInfo Culture => _culture;

    /// <inheritdoc />
    public override CalendarSystem Calendar => _calendar;
}
