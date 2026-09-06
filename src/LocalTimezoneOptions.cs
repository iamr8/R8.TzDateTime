using System.Globalization;
using NodaTime;

namespace R8.TzDateTime;

/// <summary>
///     A class representing a local timezone options.
/// </summary>
public abstract class LocalTimezoneOptions : ITimezoneInfo
{
    private static readonly Lazy<DayOfWeek[]> _unorderedDaysOfWeek = new(static () => Enum.GetValues<DayOfWeek>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private DayOfWeek[]? _orderedDaysOfWeek;

    internal ushort Index;

    // Cached description of the zone's final tzdb interval (the one with no end). Instants at or
    // after _finalIntervalStartUnixTicks — and wall times at or after the "safe" threshold, which
    // also clears the last transition's ambiguity window — resolve with plain arithmetic instead of
    // zone-interval lookups. long.MaxValue disables the fast path (zone with perpetual transitions).
    internal long FinalIntervalStartUnixTicks = long.MaxValue;
    internal long FinalIntervalOffsetTicks;
    internal long FinalIntervalSafeWallUnixTicks = long.MaxValue;

    // True when the calendar is ISO/Gregorian, whose proleptic year/month arithmetic is identical to
    // BCL DateTime math (years 1-9999) — letting calendar operations skip NodaTime entirely.
    internal bool UsesGregorianCalendar;

    /// <summary>Gets the calendar system of the timezone (e.g. Persian for Asia/Tehran).</summary>
    public abstract CalendarSystem Calendar { get; }

    /// <summary>Gets the days of the week ordered by the culture's first day of week.</summary>
    public DayOfWeek[] DaysOfWeek
    {
        get
        {
            if (_orderedDaysOfWeek != null)
                return _orderedDaysOfWeek;

            var daysOfWeek = _unorderedDaysOfWeek.Value;
            if (daysOfWeek is not { Length: 7 })
                throw new InvalidOperationException("Days of week are not valid.");

            var dayOfWeekAdjustment = Math.Abs(0 - (int)Culture.DateTimeFormat.FirstDayOfWeek);
            if (dayOfWeekAdjustment == 0)
                return _orderedDaysOfWeek = daysOfWeek;

            Span<DayOfWeek> orderedArray = stackalloc DayOfWeek[7];
            for (var i = 0; i < daysOfWeek.Length; i++)
            {
                var adjustment = (i + dayOfWeekAdjustment - 1) % 7;
                var index = adjustment >= 6
                    ? 7 - adjustment - 1
                    : adjustment + 1;
                orderedArray[i] = daysOfWeek[index];
            }

            return _orderedDaysOfWeek = orderedArray.ToArray();
        }
    }

    /// <summary>Gets the IANA ids this timezone answers to, canonical id first.</summary>
    public abstract string[] IanaIds { get; }

    /// <summary>Gets the canonical IANA id (the first configured id).</summary>
    public string DefaultIanaId => IanaIds is { Length: > 0 } ? IanaIds[0] : string.Empty;

    /// <summary>Gets the culture associated with the timezone.</summary>
    public abstract CultureInfo Culture { get; }

    /// <summary>Gets the zoned clock (zone + calendar), assigned during registration.</summary>
    public ZonedClock Clock { get; protected internal set; } = null!;
}