using System.Globalization;
using NodaTime;

namespace R8.TzDateTime.TimezoneMappers;

/// <summary>
///     Timezone options for UTC: ISO calendar and the invariant culture. This is the fallback timezone
///     and the one <see cref="TimezoneDateTime" /> values deserialize with.
/// </summary>
internal sealed class UtcTimezone : LocalTimezoneOptions
{
    /// <summary>The canonical IANA id of this timezone.</summary>
    public const string DefaultId = "UTC";
    private static readonly string[] _ianaIds = { DefaultId, "Etc/UTC", "Etc/GMT" };
    private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
    private static readonly CalendarSystem _calendar = CalendarSystem.Iso;

    /// <inheritdoc />
    public override string[] IanaIds => _ianaIds;

    /// <inheritdoc />
    public override CultureInfo Culture => _culture;

    /// <inheritdoc />
    public override CalendarSystem Calendar => _calendar;
}