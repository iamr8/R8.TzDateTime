using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;

namespace R8.TzDateTime;

/// <summary>
/// Represents a date and time that is associated with a specific timezone.
/// Provides functionality for handling operations in both UTC and local timezone contexts.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TimezoneDateTime"/> struct encapsulates date and time details with timezone awareness.
/// It supports various arithmetic operations, formatting, and timezone conversion functionalities.
/// This struct is immutable and thread-safe.
/// </para>
/// <para>
/// Date components (<see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, constructor arguments, ...)
/// are expressed in the calendar of the associated timezone — e.g. the Persian calendar for Asia/Tehran.
/// </para>
/// <para>
/// Equality and comparison operate on the represented instant only: two values pointing at the same
/// instant are equal even when their timezones differ.
/// </para>
/// <para>
/// For UTC values, week-based operations use ISO-8601 weeks (starting Monday); zoned values follow the
/// first day of week of their timezone's culture.
/// </para>
/// </remarks>
/// <threadsafety>
/// All instance members of this struct are thread-safe due to its immutable nature.
/// </threadsafety>
/// <seealso cref="IComparable"/>
/// <seealso cref="IComparable{TimezoneDateTime}"/>
/// <seealso cref="IEquatable{TimezoneDateTime}"/>
/// <seealso cref="IFormattable"/>
[JsonConverter(typeof(TimezoneDateTimeJsonConverterFactory))]
[StructLayout(LayoutKind.Sequential)]
public readonly struct TimezoneDateTime : IComparable, IComparable<TimezoneDateTime>, IEquatable<TimezoneDateTime>, IFormattable
{
    private const long _unixEpochBclTicks = 621355968000000000; // DateTime.UnixEpoch.Ticks

    private readonly ushort _timezoneIndex;

    /// <summary>
    ///     Initializes an empty instance, equal to <see cref="Empty" />: zero ticks in the UTC timezone.
    /// </summary>
    public TimezoneDateTime()
    {
        _timezoneIndex = 0;
        Ticks = 0;
    }

    /// <summary>
    ///     Initializes an instance from raw UTC ticks and a resolved timezone index. All other
    ///     constructors and operations funnel through this one.
    /// </summary>
    private TimezoneDateTime(long ticks, ushort timezoneIndex)
    {
        _timezoneIndex = timezoneIndex;
        Ticks = ticks;
    }

    /// <summary>
    ///     Initializes an instance from UTC ticks and a timezone.
    /// </summary>
    /// <param name="ticks">The number of ticks representing the UTC instant.</param>
    /// <param name="timezone">The timezone the value is associated with.</param>
    public TimezoneDateTime(long ticks, ITimezone timezone) :
        this(ticks, GetIndex(timezone))
    {
    }

    /// <summary>
    ///     Initializes an instance from a <see cref="DateTime" /> instant and a timezone.
    /// </summary>
    /// <param name="utcDateTime">
    ///     A <see cref="DateTime" /> object representing an instant. Values with
    ///     <see cref="DateTimeKind.Local" /> are converted to UTC; <see cref="DateTimeKind.Unspecified" /> values are
    ///     assumed to already be UTC.
    /// </param>
    /// <param name="timezone">A <see cref="LocalTimezone" /> object.</param>
    public TimezoneDateTime(DateTime utcDateTime, ITimezone timezone)
    {
        _timezoneIndex = GetIndex(timezone);
        Ticks = utcDateTime.Kind == DateTimeKind.Local ? utcDateTime.ToUniversalTime().Ticks : utcDateTime.Ticks;
    }

    /// <summary>
    ///     Initializes an instance from wall-clock components, interpreted in the calendar and timezone
    ///     specified. Local times that fall into a daylight-saving gap or overlap are resolved leniently.
    /// </summary>
    /// <param name="year">The year (1 through 9999).</param>
    /// <param name="month">The month (1 through 12).</param>
    /// <param name="day">The day (1 through the number of days in <paramref name="month" />).</param>
    /// <param name="hour">The hour (0 through 23).</param>
    /// <param name="minute">The minute (0 through 59).</param>
    /// <param name="second">The second (0 through 59).</param>
    /// <param name="millisecond">The millisecond (0 through 999).</param>
    /// <param name="timezone">A <see cref="LocalTimezone" /> object.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a component is outside its valid range.</exception>
    public TimezoneDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, ITimezone timezone)
    {
        if ((uint)millisecond > 999)
            throw new ArgumentOutOfRangeException(nameof(millisecond), millisecond, "Millisecond must be between 0 and 999.");

        _timezoneIndex = GetIndex(timezone);
        var localTimezone = LocalTimezone.GetTimezoneByIndex(_timezoneIndex);
        var local = new LocalDateTime(year, month, day, hour, minute, second, millisecond, localTimezone.Clock.Calendar);
        Ticks = ResolveLenientTicks(local.ToDateTimeUnspecified().Ticks, localTimezone);
    }

    /// <summary>
    ///     Initializes an instance from wall-clock date and time components, interpreted in the calendar
    ///     of the specified timezone.
    /// </summary>
    /// <param name="year">The year (1 through 9999).</param>
    /// <param name="month">The month (1 through 12).</param>
    /// <param name="day">The day (1 through the number of days in <paramref name="month" />).</param>
    /// <param name="hour">The hour (0 through 23).</param>
    /// <param name="minute">The minute (0 through 59).</param>
    /// <param name="second">The second (0 through 59).</param>
    /// <param name="timezone">A <see cref="LocalTimezone" /> object.</param>
    public TimezoneDateTime(int year, int month, int day, int hour, int minute, int second, ITimezone timezone) :
        this(year, month, day, hour, minute, second, 0, timezone)
    {
    }

    /// <summary>
    ///     Initializes an instance from wall-clock date components at midnight, interpreted in the
    ///     calendar of the specified timezone.
    /// </summary>
    /// <param name="year">The year (1 through 9999).</param>
    /// <param name="month">The month (1 through 12).</param>
    /// <param name="day">The day (1 through the number of days in <paramref name="month" />).</param>
    /// <param name="timezone">A <see cref="LocalTimezone" /> object.</param>
    public TimezoneDateTime(int year, int month, int day, ITimezone timezone)
        : this(year, month, day, 0, 0, 0, 0, timezone)
    {
    }

    /// <summary>
    ///     Gets the current date and time in the ambient timezone (<see cref="LocalTimezone.Current" />).
    /// </summary>
    public static TimezoneDateTime Now => new(DateTime.UtcNow, LocalTimezone.Current);

    /// <summary>
    ///     Gets an empty <see cref="TimezoneDateTime" /> object.
    /// </summary>
    public static TimezoneDateTime Empty => new();

    /// <inheritdoc cref="DateTime.Ticks" />
    public long Ticks { get; }

    /// <summary>
    ///     Gets whether this value belongs to the UTC timezone family (fast paths skip zone conversion).
    /// </summary>
    private bool IsUtc => _timezoneIndex == LocalTimezone.UtcIndex;

    /// <inheritdoc cref="DateTime.Year" />
    public int Year => IsUtc ? new DateTime(Ticks, DateTimeKind.Utc).Year : ToLocal().Year;

    /// <inheritdoc cref="DateTime.Month" />
    public int Month => IsUtc ? new DateTime(Ticks, DateTimeKind.Utc).Month : ToLocal().Month;

    /// <inheritdoc cref="DateTime.Day" />
    public int Day => IsUtc ? new DateTime(Ticks, DateTimeKind.Utc).Day : ToLocal().Day;

    /// <inheritdoc cref="DateTime.Hour" />
    public int Hour => TryGetWallTicks(out var wallTicks) ? (int)(wallTicks / TimeSpan.TicksPerHour % 24) : ToZoned().Hour;

    /// <inheritdoc cref="DateTime.Minute" />
    public int Minute => TryGetWallTicks(out var wallTicks) ? (int)(wallTicks / TimeSpan.TicksPerMinute % 60) : ToZoned().Minute;

    /// <inheritdoc cref="DateTime.Second" />
    public int Second => TryGetWallTicks(out var wallTicks) ? (int)(wallTicks / TimeSpan.TicksPerSecond % 60) : ToZoned().Second;

    /// <inheritdoc cref="DateTime.Millisecond" />
    public int Millisecond => TryGetWallTicks(out var wallTicks) ? (int)(wallTicks / TimeSpan.TicksPerMillisecond % 1000) : ToZoned().Millisecond;

    /// <inheritdoc cref="DateTime.DayOfWeek" />
    public DayOfWeek DayOfWeek => TryGetWallTicks(out var wallTicks)
        ? (DayOfWeek)((wallTicks / TimeSpan.TicksPerDay + 1) % 7) // 0001-01-01 is a Monday
        : ToZoned().DayOfWeek.ToDayOfWeek();

    /// <inheritdoc cref="DateTime.Date" />
    public TimezoneDateTime Date => GetStartOfDay();

    /// <summary>
    ///     Resolves the timezone index for the given timezone, using the cached index when the instance
    ///     is a <see cref="LocalTimezone" /> and falling back to a registry lookup otherwise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetIndex(ITimezone timezone)
    {
        return timezone is LocalTimezone local ? local.Index : LocalTimezone.GetTimezoneIndex(timezone.DefaultIanaId);
    }

    /// <summary>
    ///     Returns the <see cref="LocalTimezone" /> of this value via the index lookup table.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LocalTimezone GetLocalTimezone()
    {
        return LocalTimezone.GetTimezoneByIndex(_timezoneIndex);
    }

    /// <summary>
    ///     Converts this instant to a full <see cref="ZonedDateTime" />. Only used on rare fallback paths;
    ///     hot paths use <see cref="ToLocal()" /> or wall-tick arithmetic instead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ZonedDateTime ToZoned()
    {
        var clock = GetLocalTimezone().Clock;
        return GetZonedDateTimeFromTicks(Ticks, clock.Zone, clock.Calendar);
    }

    /// <summary>
    ///     Computes the wall-clock ticks of this instant in its timezone without materializing a
    ///     <see cref="ZonedDateTime" />. Returns false when the wall clock falls outside the BCL tick
    ///     range (possible only within one offset of the year 1/9999 boundaries).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetWallTicks(out long wallTicks)
    {
        if (IsUtc)
        {
            wallTicks = Ticks;
            return true;
        }

        return TryGetWallTicks(Ticks, GetLocalTimezone(), out wallTicks);
    }

    /// <summary>
    ///     Computes the wall-clock ticks of the given UTC ticks in the given timezone. Instants past the
    ///     zone's final transition use the cached constant offset and skip the zone-interval lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetWallTicks(long utcTicks, LocalTimezone timezone, out long wallTicks)
    {
        var utcUnixTicks = utcTicks - _unixEpochBclTicks;
        var offsetTicks = utcUnixTicks >= timezone.FinalIntervalStartUnixTicks
            ? timezone.FinalIntervalOffsetTicks
            : timezone.Clock.Zone.GetUtcOffset(Instant.FromUnixTimeTicks(utcUnixTicks)).Ticks;
        wallTicks = utcTicks + offsetTicks;
        return (ulong)wallTicks <= (ulong)DateTime.MaxValue.Ticks;
    }

    /// <summary>
    ///     Converts this instant to the local date and time of its timezone. Unlike ZonedDateTime — which
    ///     recomputes the calendar fields on every property access — the returned LocalDateTime computes
    ///     them once, so multi-part reads are cheaper.
    /// </summary>
    private LocalDateTime ToLocal(LocalTimezone timezone)
    {
        if (TryGetWallTicks(Ticks, timezone, out var wallTicks))
            return LocalDateTime.FromDateTime(new DateTime(wallTicks), timezone.Clock.Calendar);

        var clock = timezone.Clock;
        return GetZonedDateTimeFromTicks(Ticks, clock.Zone, clock.Calendar).LocalDateTime;
    }

    /// <summary>
    ///     Converts this instant to the local date and time of its timezone.
    /// </summary>
    private LocalDateTime ToLocal()
    {
        return ToLocal(GetLocalTimezone());
    }

    /// <summary>
    ///     Maps a local date and time to UTC ticks using NodaTime's lenient resolver. Only used on rare
    ///     fallback paths; hot paths use <see cref="ResolveLenientTicks" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long TicksAtLeniently(LocalDateTime local, DateTimeZone zone)
    {
        return GetTicksFromZonedDateTime(zone.AtLeniently(local));
    }

    /// <summary>
    ///     Returns the wall-clock start (unix ticks) of the given zone interval. Callers must guard
    ///     <see cref="ZoneInterval.HasStart" />.
    /// </summary>
    private static long WallStartUnixTicks(ZoneInterval interval)
    {
        return interval.Start.ToUnixTimeTicks() + interval.WallOffset.Ticks;
    }

    /// <summary>
    ///     Returns the wall-clock end (unix ticks) of the given zone interval. Callers must guard
    ///     <see cref="ZoneInterval.HasEnd" />.
    /// </summary>
    private static long WallEndUnixTicks(ZoneInterval interval)
    {
        return interval.End.ToUnixTimeTicks() + interval.WallOffset.Ticks;
    }

    /// <summary>
    ///     Returns whether the given wall-clock time (unix ticks) falls inside the interval's local range.
    /// </summary>
    private static bool WallContains(ZoneInterval interval, long wallUnixTicks)
    {
        if (interval.HasStart && wallUnixTicks < WallStartUnixTicks(interval))
            return false;
        if (interval.HasEnd && wallUnixTicks >= WallEndUnixTicks(interval))
            return false;
        return true;
    }

    /// <summary>
    ///     Resolves a wall-clock time (BCL ticks) to an instant with the same semantics as
    ///     <c>zone.AtLeniently(...)</c> — ambiguous times map to the earlier offset, skipped times are
    ///     shifted forward by the gap (equivalent to applying the pre-transition offset) — but without
    ///     allocating NodaTime's ZoneLocalMapping. Wall times past the zone's final transition resolve
    ///     with plain arithmetic. Pinned against NodaTime by TimezoneResolverEquivalenceTests.
    /// </summary>
    private static long ResolveLenientTicks(long wallTicks, LocalTimezone timezone)
    {
        var wallUnix = wallTicks - _unixEpochBclTicks;
        if (wallUnix >= timezone.FinalIntervalSafeWallUnixTicks)
            return wallTicks - timezone.FinalIntervalOffsetTicks;

        return ResolveLenientTicksCore(wallTicks, timezone.Clock.Zone);
    }

    /// <summary>
    ///     Zone-agnostic core of <see cref="ResolveLenientTicks" />: works against any
    ///     <see cref="DateTimeZone" /> regardless of offset sign, granularity, gap size or transition
    ///     cadence. Internal so tests can verify it against arbitrary (unregistered) tzdb zones.
    /// </summary>
    internal static long ResolveLenientTicksCore(long wallTicks, DateTimeZone zone)
    {
        var wallUnix = wallTicks - _unixEpochBclTicks;
        var guess = zone.GetZoneInterval(Instant.FromUnixTimeTicks(wallUnix));

        if (WallContains(guess, wallUnix))
        {
            if (guess.HasStart)
            {
                var earlier = zone.GetZoneInterval(guess.Start.Minus(Duration.Epsilon));
                if (WallContains(earlier, wallUnix))
                    return wallTicks - earlier.WallOffset.Ticks; // ambiguous: the earlier interval wins
            }

            return wallTicks - guess.WallOffset.Ticks;
        }

        if (guess.HasStart && wallUnix < WallStartUnixTicks(guess))
        {
            // Neighbor below the guess, or a gap; both resolve with the pre-transition offset.
            var earlier = zone.GetZoneInterval(guess.Start.Minus(Duration.Epsilon));
            return wallTicks - earlier.WallOffset.Ticks;
        }

        var later = zone.GetZoneInterval(guess.End);
        return WallContains(later, wallUnix)
            ? wallTicks - later.WallOffset.Ticks
            : wallTicks - guess.WallOffset.Ticks; // gap: the guess is the pre-transition interval
    }

    /// <summary>
    ///     Resolves a wall-clock midnight (BCL ticks) to the first instant of that day with the same
    ///     semantics as <c>LocalDate.AtStartOfDayInZone(...)</c> — a skipped midnight maps to the first
    ///     valid instant after the gap — without allocating NodaTime's ZoneLocalMapping. Wall times past
    ///     the zone's final transition resolve with plain arithmetic.
    /// </summary>
    private static long ResolveStartOfDayTicks(long wallMidnightTicks, LocalTimezone timezone)
    {
        var wallUnix = wallMidnightTicks - _unixEpochBclTicks;
        if (wallUnix >= timezone.FinalIntervalSafeWallUnixTicks)
            return wallMidnightTicks - timezone.FinalIntervalOffsetTicks;

        return ResolveStartOfDayTicksCore(wallMidnightTicks, timezone.Clock.Zone);
    }

    /// <summary>
    ///     Zone-agnostic core of <see cref="ResolveStartOfDayTicks" />: works against any
    ///     <see cref="DateTimeZone" /> including gaps that span entire days. Internal so tests can verify
    ///     it against arbitrary (unregistered) tzdb zones.
    /// </summary>
    internal static long ResolveStartOfDayTicksCore(long wallMidnightTicks, DateTimeZone zone)
    {
        var wallUnix = wallMidnightTicks - _unixEpochBclTicks;
        var guess = zone.GetZoneInterval(Instant.FromUnixTimeTicks(wallUnix));

        if (WallContains(guess, wallUnix))
        {
            if (guess.HasStart)
            {
                var earlier = zone.GetZoneInterval(guess.Start.Minus(Duration.Epsilon));
                if (WallContains(earlier, wallUnix))
                    return wallMidnightTicks - earlier.WallOffset.Ticks; // ambiguous midnight: earlier occurrence
            }

            return wallMidnightTicks - guess.WallOffset.Ticks;
        }

        if (guess.HasStart && wallUnix < WallStartUnixTicks(guess))
        {
            var earlier = zone.GetZoneInterval(guess.Start.Minus(Duration.Epsilon));
            return WallContains(earlier, wallUnix)
                ? wallMidnightTicks - earlier.WallOffset.Ticks
                : guess.Start.ToUnixTimeTicks() + _unixEpochBclTicks; // skipped midnight: first instant after the gap
        }

        var later = zone.GetZoneInterval(guess.End);
        return WallContains(later, wallUnix)
            ? wallMidnightTicks - later.WallOffset.Ticks
            : guess.End.ToUnixTimeTicks() + _unixEpochBclTicks; // skipped midnight: first instant after the gap
    }

    /// <summary>
    ///     Returns the represented instant as a UTC <see cref="DateTime" />.
    /// </summary>
    public DateTime GetUtcDateTime()
    {
        return new DateTime(Ticks, DateTimeKind.Utc);
    }

    /// <summary>
    ///     Returns the wall-clock date and time of this value in its timezone, with
    ///     <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    public DateTime GetDateTime()
    {
        if (TryGetWallTicks(out var wallTicks))
            return new DateTime(wallTicks, DateTimeKind.Unspecified);

        return ToZoned().ToDateTimeUnspecified();
    }

    /// <summary>
    ///     Returns the timezone of this instance.
    /// </summary>
    /// <returns>A <see cref="LocalTimezone" /> object</returns>
    /// <exception cref="InvalidOperationException">Thrown when timezone is not available.</exception>
    public ITimezone GetTimezone()
    {
        return GetLocalTimezone();
    }

    /// <summary>
    ///     Returns the number of days in the month and year of the specified date, in the timezone's calendar.
    /// </summary>
    /// <returns>An integer from 28 through 31.</returns>
    public int GetDaysInMonth()
    {
        if (IsUtc)
        {
            var dateTime = new DateTime(Ticks, DateTimeKind.Utc);
            return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
        }

        var local = ToLocal();
        return local.Calendar.GetDaysInMonth(local.Year, local.Month);
    }

    /// <summary>
    ///     Returns a new object with the same date and hour as this instance, and the minute, second and smaller components
    ///     set to zero. For instance, "2019-01-01 12:33:25" becomes "2019-01-01 12:00:00".
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfHour()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - Ticks % TimeSpan.TicksPerHour, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveLenientTicks(wall - wall % TimeSpan.TicksPerHour, timezone), _timezoneIndex);

        return new TimezoneDateTime(TicksAtLeniently(ToZoned().LocalDateTime.With(TimeAdjusters.TruncateToHour), timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object representing the last tick of the current hour. For instance, "2019-01-01 12:33:25"
    ///     becomes "2019-01-01 12:59:59.9999999" — exactly one tick before the start of the next hour.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetEndOfHour()
    {
        return new TimezoneDateTime(GetStartOfNextHour().Ticks - 1, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object with the same date, hour and minute as this instance, and the second and smaller components
    ///     set to zero. For instance, "2019-01-01 12:33:25" becomes "2019-01-01 12:33:00".
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfMinute()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - Ticks % TimeSpan.TicksPerMinute, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveLenientTicks(wall - wall % TimeSpan.TicksPerMinute, timezone), _timezoneIndex);

        return new TimezoneDateTime(TicksAtLeniently(ToZoned().LocalDateTime.With(TimeAdjusters.TruncateToMinute), timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object representing the last tick of the current minute. For instance, "2019-01-01 12:33:25"
    ///     becomes "2019-01-01 12:33:59.9999999" — exactly one tick before the start of the next minute.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetEndOfMinute()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - (Ticks % TimeSpan.TicksPerMinute) + TimeSpan.TicksPerMinute - 1, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveLenientTicks(wall - wall % TimeSpan.TicksPerMinute + TimeSpan.TicksPerMinute, timezone) - 1, _timezoneIndex);

        return new TimezoneDateTime(TicksAtLeniently(ToZoned().LocalDateTime.PlusMinutes(1).With(TimeAdjusters.TruncateToMinute), timezone.Clock.Zone) - 1, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object with the same date as this instance, and the time value set to 00:00:00. For instance, if the
    ///     current instance represents the date "2019-01-01 12:33:25", this method will return a new instance representing
    ///     "2019-01-01 00:00:00". When midnight is skipped by a daylight-saving transition, the first valid
    ///     time of the day is returned.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    /// <remarks>This method works similar to <c>DateTime.Date</c></remarks>
    public TimezoneDateTime GetStartOfDay()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - (Ticks % TimeSpan.TicksPerDay), _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveStartOfDayTicks(wall - (wall % TimeSpan.TicksPerDay), timezone), _timezoneIndex);

        return new TimezoneDateTime(GetTicksFromZonedDateTime(ToZoned().Date.AtStartOfDayInZone(timezone.Clock.Zone)), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a object for first moment of the next hour.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfNextHour()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - Ticks % TimeSpan.TicksPerHour + TimeSpan.TicksPerHour, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveLenientTicks(wall - wall % TimeSpan.TicksPerHour + TimeSpan.TicksPerHour, timezone), _timezoneIndex);

        return new TimezoneDateTime(TicksAtLeniently(ToZoned().LocalDateTime.PlusHours(1).With(TimeAdjusters.TruncateToHour), timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a object for first moment of the next day.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfNextDay()
    {
        if (IsUtc)
            return new TimezoneDateTime(Ticks - Ticks % TimeSpan.TicksPerDay + TimeSpan.TicksPerDay, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveStartOfDayTicks(wall - wall % TimeSpan.TicksPerDay + TimeSpan.TicksPerDay, timezone), _timezoneIndex);

        return new TimezoneDateTime(GetTicksFromZonedDateTime(ToZoned().Date.PlusDays(1).AtStartOfDayInZone(timezone.Clock.Zone)), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object representing the last tick of the current day. For instance, "2019-01-01 12:33:25"
    ///     becomes "2019-01-01 23:59:59.9999999" — exactly one tick before the start of the next day.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetEndOfDay()
    {
        return new TimezoneDateTime(GetStartOfNextDay().Ticks - 1, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object similar to this instance, and the day value set to 1. For instance, if the current instance
    ///     represents the date "2019-01-15 12:33:25", this method will return a new instance representing "2019-01-01
    ///     00:00:00".
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfMonth()
    {
        if (IsUtc)
        {
            var dateTime = new DateTime(Ticks, DateTimeKind.Utc);
            return new TimezoneDateTime(new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc).Ticks, _timezoneIndex);
        }

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall) && timezone.UsesGregorianCalendar)
        {
            var wallDateTime = new DateTime(wall);
            var firstOfMonthWall = new DateTime(wallDateTime.Year, wallDateTime.Month, 1).Ticks;
            return new TimezoneDateTime(ResolveStartOfDayTicks(firstOfMonthWall, timezone), _timezoneIndex);
        }

        var local = ToLocal(timezone);
        var firstDay = new LocalDate(local.Year, local.Month, 1, local.Calendar);
        return new TimezoneDateTime(ResolveStartOfDayTicks(firstDay.AtMidnight().ToDateTimeUnspecified().Ticks, timezone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object of the first moment of the next month.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfNextMonth()
    {
        if (IsUtc)
        {
            var dateTime = new DateTime(Ticks, DateTimeKind.Utc);
            return new TimezoneDateTime(new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).Ticks, _timezoneIndex);
        }

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall) && timezone.UsesGregorianCalendar)
        {
            var wallDateTime = new DateTime(wall);
            var firstOfNextMonthWall = new DateTime(wallDateTime.Year, wallDateTime.Month, 1).AddMonths(1).Ticks;
            return new TimezoneDateTime(ResolveStartOfDayTicks(firstOfNextMonthWall, timezone), _timezoneIndex);
        }

        var local = ToLocal(timezone);
        var firstDayOfNextMonth = new LocalDate(local.Year, local.Month, 1, local.Calendar).PlusMonths(1);
        return new TimezoneDateTime(ResolveStartOfDayTicks(firstDayOfNextMonth.AtMidnight().ToDateTimeUnspecified().Ticks, timezone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object similar to this instance, and the day value set to the last day of the month, at the last
    ///     tick of that day — exactly one tick before the start of the next month.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetEndOfMonth()
    {
        return new TimezoneDateTime(GetStartOfNextMonth().Ticks - 1, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object similar to this instance, and represents the first moment of the current week. UTC values
    ///     use ISO-8601 weeks (starting Monday); zoned values follow their culture's first day of week.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfWeek()
    {
        if (IsUtc)
            return new TimezoneDateTime(GetUtcStartOfWeekTicks(Ticks), _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveStartOfDayTicks(GetWallStartOfWeekTicks(wall, timezone), timezone), _timezoneIndex);

        var local = ToZoned().LocalDateTime;
        var startOfWeekDate = local.Date.PlusDays(-GetDaysSinceStartOfWeek(local, timezone));
        return new TimezoneDateTime(GetTicksFromZonedDateTime(startOfWeekDate.AtStartOfDayInZone(timezone.Clock.Zone)), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object of the first moment of the next week.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetStartOfNextWeek()
    {
        if (IsUtc)
            return new TimezoneDateTime(GetUtcStartOfWeekTicks(Ticks) + (7 * TimeSpan.TicksPerDay), _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
            return new TimezoneDateTime(ResolveStartOfDayTicks(GetWallStartOfWeekTicks(wall, timezone) + (7 * TimeSpan.TicksPerDay), timezone), _timezoneIndex);

        var local = ToZoned().LocalDateTime;
        var startOfNextWeekDate = local.Date.PlusDays(7 - GetDaysSinceStartOfWeek(local, timezone));
        return new TimezoneDateTime(GetTicksFromZonedDateTime(startOfNextWeekDate.AtStartOfDayInZone(timezone.Clock.Zone)), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object representing the last tick of the current week — exactly one tick before the start of the
    ///     next week.
    /// </summary>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime GetEndOfWeek()
    {
        return new TimezoneDateTime(GetStartOfNextWeek().Ticks - 1, _timezoneIndex);
    }

    /// <summary>
    ///     Returns the UTC ticks of the ISO-8601 week start (Monday) for the given UTC ticks.
    /// </summary>
    private static long GetUtcStartOfWeekTicks(long ticks)
    {
        // UTC values use ISO-8601 weeks (Monday-first). 0001-01-01 is a Monday, so the
        // day number modulo 7 is the distance from the start of the week.
        var startOfDay = ticks - (ticks % TimeSpan.TicksPerDay);
        var daysSinceMonday = (startOfDay / TimeSpan.TicksPerDay) % 7;
        return startOfDay - (daysSinceMonday * TimeSpan.TicksPerDay);
    }

    /// <summary>
    ///     Returns how many days the given local date is past the culture's first day of week.
    /// </summary>
    private static int GetDaysSinceStartOfWeek(LocalDateTime local, LocalTimezone timezone)
    {
        var firstDayOfWeek = timezone.Culture.DateTimeFormat.FirstDayOfWeek;
        return (7 + (local.DayOfWeek.ToDayOfWeek() - firstDayOfWeek)) % 7;
    }

    /// <summary>
    ///     Returns the wall-clock midnight ticks of the week start containing the given wall-clock ticks,
    ///     honoring the timezone culture's first day of week.
    /// </summary>
    private static long GetWallStartOfWeekTicks(long wallTicks, LocalTimezone timezone)
    {
        var midnight = wallTicks - (wallTicks % TimeSpan.TicksPerDay);
        var dayOfWeek = (DayOfWeek)(((midnight / TimeSpan.TicksPerDay) + 1) % 7); // 0001-01-01 is a Monday
        var diff = (7 + (dayOfWeek - timezone.Culture.DateTimeFormat.FirstDayOfWeek)) % 7;
        return midnight - (diff * TimeSpan.TicksPerDay);
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of years to the value of this
    ///     instance. When the resulting date does not exist (e.g. a leap day), the day is clamped to the last valid day of
    ///     the month, like <see cref="DateTime.AddYears" />.
    /// </summary>
    /// <param name="value">A number of years. The value parameter can be negative or positive.</param>
    public TimezoneDateTime AddYears(int value)
    {
        if (value == 0)
            return this;

        if (IsUtc)
            return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddYears(value).Ticks, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
        {
            // Gregorian zones: BCL year math is identical (incl. leap-day clamping) and much faster.
            var targetWall = timezone.UsesGregorianCalendar
                ? new DateTime(wall).AddYears(value).Ticks
                : GetWallDatePlusYears(wall, value, timezone);
            return new TimezoneDateTime(ResolveLenientTicks(targetWall, timezone), _timezoneIndex);
        }

        var local = ToZoned().LocalDateTime;
        var target = local.Date.PlusYears(value).At(local.TimeOfDay);
        return new TimezoneDateTime(TicksAtLeniently(target, timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Shifts the date part of the given wall-clock ticks by whole years in the timezone's calendar,
    ///     preserving the time of day; the day is clamped to the target month length.
    /// </summary>
    private static long GetWallDatePlusYears(long wallTicks, int value, LocalTimezone timezone)
    {
        var tickOfDay = wallTicks % TimeSpan.TicksPerDay;
        var targetDate = LocalDate.FromDateTime(new DateTime(wallTicks), timezone.Clock.Calendar).PlusYears(value);
        return targetDate.AtMidnight().ToDateTimeUnspecified().Ticks + tickOfDay;
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of months to the value of this
    ///     instance. When the resulting date does not exist, the day is clamped to the last valid day of the month, like
    ///     <see cref="DateTime.AddMonths" />.
    /// </summary>
    /// <param name="value">A number of months. The value parameter can be negative or positive.</param>
    public TimezoneDateTime AddMonths(int value)
    {
        if (value == 0)
            return this;

        if (IsUtc)
            return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddMonths(value).Ticks, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
        {
            // Gregorian zones: BCL month math is identical (incl. month-length clamping) and much faster.
            var targetWall = timezone.UsesGregorianCalendar
                ? new DateTime(wall).AddMonths(value).Ticks
                : GetWallDatePlusMonths(wall, value, timezone);
            return new TimezoneDateTime(ResolveLenientTicks(targetWall, timezone), _timezoneIndex);
        }

        var local = ToZoned().LocalDateTime;
        var target = local.Date.PlusMonths(value).At(local.TimeOfDay);
        return new TimezoneDateTime(TicksAtLeniently(target, timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Shifts the date part of the given wall-clock ticks by whole months in the timezone's calendar,
    ///     preserving the time of day; the day is clamped to the target month length.
    /// </summary>
    private static long GetWallDatePlusMonths(long wallTicks, int value, LocalTimezone timezone)
    {
        var tickOfDay = wallTicks % TimeSpan.TicksPerDay;
        var targetDate = LocalDate.FromDateTime(new DateTime(wallTicks), timezone.Clock.Calendar).PlusMonths(value);
        return targetDate.AtMidnight().ToDateTimeUnspecified().Ticks + tickOfDay;
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of days to the value of this instance,
    ///     preserving the local time of day. Local times that fall into a daylight-saving gap or overlap are resolved
    ///     leniently instead of throwing.
    /// </summary>
    /// <param name="value">A number of days. The value parameter can be negative or positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result falls outside the supported date range.</exception>
    public TimezoneDateTime AddDays(int value)
    {
        if (value == 0)
            return this;

        if (IsUtc)
            return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddDays(value).Ticks, _timezoneIndex);

        var timezone = GetLocalTimezone();
        if (TryGetWallTicks(Ticks, timezone, out var wall))
        {
            // Days in the BCL tick range; guards the multiply and the target range like DateTime.AddDays.
            const int maxDays = 3_652_059;
            if (value is > maxDays or < -maxDays)
                throw new ArgumentOutOfRangeException(nameof(value));

            var target = wall + (value * TimeSpan.TicksPerDay);
            if ((ulong)target > (ulong)DateTime.MaxValue.Ticks)
                throw new ArgumentOutOfRangeException(nameof(value));

            return new TimezoneDateTime(ResolveLenientTicks(target, timezone), _timezoneIndex);
        }

        var local = ToZoned().LocalDateTime;
        var fallbackTarget = local.Date.PlusDays(value).At(local.TimeOfDay);
        return new TimezoneDateTime(TicksAtLeniently(fallbackTarget, timezone.Clock.Zone), _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of hours to the value of this
    ///     instance.
    /// </summary>
    /// <param name="value">A number of hours. The value parameter can be negative or positive.</param>
    public TimezoneDateTime AddHours(int value)
    {
        if (value == 0)
            return this;

        // Elapsed-time arithmetic is timezone-independent; DateTime.AddHours also validates the range.
        return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddHours(value).Ticks, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of minutes to the value of this
    ///     instance.
    /// </summary>
    /// <param name="value">A number of minutes. The value parameter can be negative or positive.</param>
    public TimezoneDateTime AddMinutes(int value)
    {
        if (value == 0)
            return this;

        return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddMinutes(value).Ticks, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new <see cref="TimezoneDateTime" /> that adds the specified number of seconds to the value of this
    ///     instance.
    /// </summary>
    /// <param name="value">A number of seconds. The value parameter can be negative or positive.</param>
    public TimezoneDateTime AddSeconds(int value)
    {
        if (value == 0)
            return this;

        return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).AddSeconds(value).Ticks, _timezoneIndex);
    }

    /// <summary>
    ///     Returns a new object with the specified timezone, representing the same instant.
    /// </summary>
    /// <param name="timezone">A timezone to be compared with the current data</param>
    /// <returns>A <see cref="TimezoneDateTime" /> object</returns>
    public TimezoneDateTime WithTimezone(LocalTimezone timezone)
    {
        return new TimezoneDateTime(Ticks, timezone.Index);
    }

    /// <summary>
    ///     Converts a <see cref="DateTime" /> to a <see cref="TimezoneDateTime" />.
    /// </summary>
    /// <param name="dateTime">
    ///     A <see cref="DateTime" /> to convert. Values with <see cref="DateTimeKind.Local" /> are converted to UTC;
    ///     <see cref="DateTimeKind.Unspecified" /> values are assumed to already be UTC.
    /// </param>
    /// <param name="timezone">A <see cref="LocalTimezone" /> to convert.</param>
    /// <returns>A <see cref="TimezoneDateTime" />.</returns>
    public static TimezoneDateTime FromDateTime(DateTime dateTime, LocalTimezone timezone)
    {
        return new TimezoneDateTime(dateTime, timezone);
    }

    /// <summary>
    ///     Converts a <see cref="ZonedDateTime" /> to BCL UTC ticks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetTicksFromZonedDateTime(ZonedDateTime zonedDateTime)
    {
        return zonedDateTime.ToInstant().ToUnixTimeTicks() + _unixEpochBclTicks;
    }

    /// <summary>
    ///     Converts BCL UTC ticks to a <see cref="ZonedDateTime" /> in the given zone and calendar.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ZonedDateTime GetZonedDateTimeFromTicks(long ticks, DateTimeZone zone, CalendarSystem calendar)
    {
        var currentInstant = Instant.FromUnixTimeTicks(ticks - _unixEpochBclTicks);
        return currentInstant.InZone(zone, calendar);
    }

    /// <summary>
    ///     Returns a string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format,
    ///     compared against the current UTC time.
    /// </summary>
    /// <param name="format">The format to use when the time is not relative.</param>
    /// <returns>A string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.</returns>
    public string Humanize(string format = "G")
    {
        return Humanize(DateTime.UtcNow, maxRelativity: null, format);
    }

    /// <summary>
    ///     Returns a string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format,
    ///     compared against the current UTC time.
    /// </summary>
    /// <param name="maxRelativity">The maximum time span to consider as relative. If null, all time spans will be considered.</param>
    /// <param name="format">The format to use when the time is not relative.</param>
    /// <returns>A string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.</returns>
    public string Humanize(TimeSpan maxRelativity, string format = "G")
    {
        return Humanize(DateTime.UtcNow, maxRelativity, format);
    }

    /// <summary>
    ///     Returns a string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.
    /// </summary>
    /// <param name="compareAgainst">A <see cref="TimezoneDateTime" /> object to be compared as the current date and time.</param>
    /// <param name="maxRelativity">The maximum time span to consider as relative. If null, all time spans will be considered.</param>
    /// <param name="format">The format to use when the time is not relative.</param>
    /// <returns>A string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.</returns>
    public string Humanize(TimezoneDateTime compareAgainst, TimeSpan? maxRelativity, string format = "G")
    {
        return Humanize(compareAgainst.GetUtcDateTime(), maxRelativity, format);
    }

    /// <summary>
    ///     Returns a string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.
    /// </summary>
    /// <param name="compareAgainst">A UTC <see cref="DateTime" /> object to be compared as the current date and time.</param>
    /// <param name="maxRelativity">The maximum time span to consider as relative. If null, all time spans will be considered.</param>
    /// <param name="format">The format to use when the time is not relative.</param>
    /// <exception cref="ArgumentException">When <paramref name="compareAgainst" /> is not UTC.</exception>
    /// <returns>A string that represents the current <see cref="TimezoneDateTime" /> instance in a relative time format.</returns>
    public string Humanize(DateTime compareAgainst, TimeSpan? maxRelativity, string format = "G")
    {
        if (compareAgainst.Kind != DateTimeKind.Utc)
            throw new ArgumentException("compareAgainst must be UTC", nameof(compareAgainst));

        var comparingTzdt = new TimezoneDateTime(compareAgainst.Ticks, _timezoneIndex);
        var relative = comparingTzdt < this
            ? HumanizeFuture(this - comparingTzdt, comparingTzdt)
            : HumanizePast(comparingTzdt, maxRelativity);

        return relative ?? ToString(format, null);
    }

    /// <summary>
    ///     Returns the relative phrase for an instant in the future, or null when the gap is too large to
    ///     express relatively (the caller falls back to the formatted value).
    /// </summary>
    private string? HumanizeFuture(TimeSpan duration, TimezoneDateTime comparingTzdt)
    {
        if (duration.TotalSeconds < 60)
            return "in a few seconds";
        if (duration.TotalMinutes < 60)
            return $"in {duration.Minutes} minutes";
        if (duration.TotalHours < 6)
            return $"in {duration.Hours} hours";
        if (duration.TotalHours < 24)
        {
            return GetLocalDate() == comparingTzdt.GetLocalDate()
                ? $"in {duration.Hours} hours"
                : $"tomorrow at {ToString("hh:mm tt", null)}";
        }

        if (duration.TotalDays < 7)
            return "soon";
        if (duration.TotalDays < 30)
            return "next month";
        if (duration.TotalDays < 365)
            return "in future";

        return null;
    }

    /// <summary>
    ///     Returns the relative phrase for an instant in the past, or null when the gap exceeds
    ///     <paramref name="maxRelativity" /> or is too large to express relatively.
    /// </summary>
    private string? HumanizePast(TimezoneDateTime comparingTzdt, TimeSpan? maxRelativity)
    {
        var duration = comparingTzdt - this;
        if (maxRelativity != null && !(duration <= maxRelativity))
            return null;

        return HumanizeRecentPast(duration) ?? HumanizeDistantPast(duration, comparingTzdt);
    }

    /// <summary>
    ///     Returns the relative phrase for a past instant within the last twelve hours, or null beyond that.
    /// </summary>
    private static string? HumanizeRecentPast(TimeSpan duration)
    {
        var seconds = (int)duration.TotalSeconds;
        switch (seconds)
        {
            case <= 30:
                return "just now";
            case <= 60:
                return "a few seconds ago";
        }

        var minutes = (int)duration.TotalMinutes;
        switch (minutes)
        {
            case <= 10:
                return "a few minutes ago";
            case < 60:
                return $"{minutes} minutes ago";
        }

        var hours = (int)duration.TotalHours;
        return hours switch
        {
            <= 1 => "an hour ago",
            <= 5 => $"{hours} hours ago",
            <= 12 => "a few hours ago",
            _ => null,
        };
    }

    /// <summary>
    ///     Returns the relative phrase for a past instant a day or more ago, or null when it is more than a
    ///     year in the past.
    /// </summary>
    private string? HumanizeDistantPast(TimeSpan duration, TimezoneDateTime comparingTzdt)
    {
        var (currentYear, currentMonth, currentDay) = GetLocalDate();
        var (compareYear, compareMonth, compareDay) = comparingTzdt.GetLocalDate();

        var days = (int)duration.TotalDays;
        var weeks = days / 7;
        switch (weeks)
        {
            case 0:
                switch (days)
                {
                    case <= 1:
                    {
                        // The wall-clock time in this value's timezone; never the host machine's timezone.
                        var currentUnspecified = GetDateTime();
                        return currentYear == compareYear && currentMonth == compareMonth && currentDay == compareDay
                            ? $"today at {ToString(currentUnspecified, "hh:mm tt")}"
                            : $"yesterday at {ToString(currentUnspecified, "hh:mm tt")}";
                    }
                    case <= 3:
                        return $"{days} days ago";
                    default:
                        return "a few days ago";
                }
            case 1:
                return "last week";
        }

        var months = currentYear == compareYear
            ? compareMonth - currentMonth
            : (int)(duration.TotalDays / 30);
        switch (months)
        {
            case < 1:
                return "a few weeks ago";
            case 1:
                return "last month";
            case <= 12:
            {
                if (months >= 6 && currentYear == compareYear - 1)
                    return "last year";

                return $"{months} months ago";
            }
        }

        if (currentYear == compareYear - 1)
            return "last year";

        return null;
    }

    /// <summary>
    ///     Returns the calendar date components of this value in its timezone.
    /// </summary>
    private (int Year, int Month, int Day) GetLocalDate()
    {
        if (IsUtc)
        {
            var dateTime = new DateTime(Ticks, DateTimeKind.Utc);
            return (dateTime.Year, dateTime.Month, dateTime.Day);
        }

        var local = ToLocal();
        return (local.Year, local.Month, local.Day);
    }

    /// <summary>
    ///     Checks if the current instance is today.
    /// </summary>
    public bool IsToday()
    {
        return IsToday(DateTime.UtcNow);
    }

    /// <summary>
    ///     Checks if the current instance falls on the same calendar day as the specified UTC instant, evaluated in this
    ///     instance's timezone.
    /// </summary>
    /// <param name="utcNow">The UTC instant to treat as "now".</param>
    /// <exception cref="ArgumentException">When <paramref name="utcNow" /> is not UTC.</exception>
    public bool IsToday(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
            throw new ArgumentException("utcNow must be UTC", nameof(utcNow));

        if (IsUtc)
            return Ticks / TimeSpan.TicksPerDay == utcNow.Ticks / TimeSpan.TicksPerDay;

        return GetLocalDate() == new TimezoneDateTime(utcNow.Ticks, _timezoneIndex).GetLocalDate();
    }

    /// <summary>
    ///     Adds the specified time span to the current instance and returns a new <see cref="TimezoneDateTime" /> object.
    /// </summary>
    public TimezoneDateTime Add(TimeSpan timeSpan)
    {
        return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).Add(timeSpan).Ticks, _timezoneIndex);
    }

    /// <summary>
    ///     Subtracts the specified time span from the current instance and returns a new <see cref="TimezoneDateTime" />
    ///     object.
    /// </summary>
    public TimezoneDateTime Subtract(TimeSpan timeSpan)
    {
        return new TimezoneDateTime(new DateTime(Ticks, DateTimeKind.Utc).Subtract(timeSpan).Ticks, _timezoneIndex);
    }

    /// <summary>
    ///     Subtracts the specified <see cref="TimezoneDateTime" /> from the current instance and returns a
    ///     <see cref="TimeSpan" />
    /// </summary>
    public TimeSpan Subtract(TimezoneDateTime timezoneDateTime)
    {
        return new TimeSpan(Ticks - timezoneDateTime.Ticks);
    }

    /// <summary>
    ///     Returns whether this instance represents a date and time within daylight saving time
    ///     for the current timezone.
    /// </summary>
    /// <returns>true if the value of this instance is within daylight saving time for the current timezone; otherwise, false.</returns>
    public bool IsDaylightSavingTime()
    {
        if (IsUtc)
            return false;

        var zone = GetLocalTimezone().Clock.Zone;
        var instant = Instant.FromUnixTimeTicks(Ticks - _unixEpochBclTicks);
        var interval = zone.GetZoneInterval(instant);
        return interval.Savings != Offset.Zero;
    }

    /// <summary>
    ///     Returns the wall-clock representation of this value formatted with the general ("G") format of
    ///     its timezone's culture.
    /// </summary>
    public override string ToString()
    {
        return ToString(GetDateTime(), "G");
    }

    /// <summary>
    ///     Returns the wall-clock representation of this value formatted with the given format. The
    ///     provider is used when it is a <see cref="CultureInfo" />; otherwise the timezone's culture applies.
    /// </summary>
    /// <param name="format">A standard or custom date and time format string.</param>
    public string ToString(string? format)
    {
        return ToString(GetDateTime(), format, formatProvider: null);
    }

    /// <summary>
    ///     Returns the wall-clock representation of this value formatted with the given format. The
    ///     provider is used when it is a <see cref="CultureInfo" />; otherwise the timezone's culture applies.
    /// </summary>
    /// <param name="format">A standard or custom date and time format string.</param>
    /// <param name="formatProvider">An optional culture override.</param>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString(GetDateTime(), format, formatProvider);
    }

    /// <summary>
    ///     Formats a wall-clock <see cref="DateTime" /> with the timezone's culture (right-to-left marks
    ///     stripped for RTL cultures).
    /// </summary>
    private string ToString(DateTime dateTimeUnspecified, string? format, IFormatProvider? formatProvider = null)
    {
        if (string.IsNullOrEmpty(format))
            format = "G";

        var timezone = GetLocalTimezone();
        var culture = formatProvider as CultureInfo ?? timezone.Culture;

        var str = dateTimeUnspecified.ToString(format, culture);
        if (culture.TextInfo.IsRightToLeft)
        {
            str = RemoveRlmChar(str);
        }

        return str;
    }

    private static string RemoveRlmChar(string str)
    {
        if (!str.Contains('\u200F', StringComparison.Ordinal))
            return str;

        Span<char> c = stackalloc char[str.Length];
        var lastIndex = -1;
        for (var index = 0; index < str.Length; index++)
        {
            var ch = str[index];
            if (ch == '\u200F')
                continue;

            c[++lastIndex] = ch;
        }

        c = c[..(lastIndex + 1)];
        return new string(c);
    }

    /// <summary>
    ///     Compares two values by their instants.
    /// </summary>
    public static int Compare(TimezoneDateTime left, TimezoneDateTime right)
    {
        return left.Ticks.CompareTo(right.Ticks);
    }

    /// <summary>
    ///     Compares this value to a boxed <see cref="TimezoneDateTime" /> by instant.
    /// </summary>
    /// <exception cref="ArgumentException">When <paramref name="obj" /> is not a <see cref="TimezoneDateTime" />.</exception>
    public int CompareTo(object? obj)
    {
        if (obj == null)
            return 1;
        if (obj is TimezoneDateTime tz)
            return Compare(this, tz);
        throw new ArgumentException("Argument must be TimezoneDateTime", nameof(obj));
    }

    /// <summary>
    ///     Compares this value to another by instant.
    /// </summary>
    public int CompareTo(TimezoneDateTime value)
    {
        return Compare(this, value);
    }

    /// <summary>Returns whether <paramref name="a" /> represents a later instant than <paramref name="b" />.</summary>
    public static bool operator >(TimezoneDateTime a, TimezoneDateTime b)
    {
        return a.Ticks > b.Ticks;
    }

    /// <summary>Returns the elapsed time between the two instants.</summary>
    public static TimeSpan operator -(TimezoneDateTime a, TimezoneDateTime b)
    {
        return a.Subtract(b);
    }

    /// <summary>Adds the given time span to the instant, keeping the timezone.</summary>
    public static TimezoneDateTime operator +(TimezoneDateTime d, TimeSpan t)
    {
        return d.Add(t);
    }

    /// <summary>Subtracts the given time span from the instant, keeping the timezone.</summary>
    public static TimezoneDateTime operator -(TimezoneDateTime d, TimeSpan t)
    {
        return d.Subtract(t);
    }

    /// <summary>Returns whether <paramref name="a" /> represents an earlier instant than <paramref name="b" />.</summary>
    public static bool operator <(TimezoneDateTime a, TimezoneDateTime b)
    {
        return a.Ticks < b.Ticks;
    }

    /// <summary>Returns whether <paramref name="a" /> is the same instant as or later than <paramref name="b" />.</summary>
    public static bool operator >=(TimezoneDateTime a, TimezoneDateTime b)
    {
        return a.Ticks >= b.Ticks;
    }

    /// <summary>Returns whether <paramref name="a" /> is the same instant as or earlier than <paramref name="b" />.</summary>
    public static bool operator <=(TimezoneDateTime a, TimezoneDateTime b)
    {
        return a.Ticks <= b.Ticks;
    }

    /// <summary>
    ///     Returns whether the two values represent the same instant, regardless of timezone.
    /// </summary>
    public static bool Equals(TimezoneDateTime left, TimezoneDateTime right)
    {
        return left.Ticks == right.Ticks;
    }

    /// <summary>Returns whether the two values represent the same instant, regardless of timezone.</summary>
    public static bool operator ==(TimezoneDateTime a, TimezoneDateTime b)
    {
        return Equals(a, b);
    }

    /// <summary>
    ///     Returns whether this value represents the same instant as the other, regardless of timezone.
    /// </summary>
    public bool Equals(TimezoneDateTime other)
    {
        return Equals(this, other);
    }

    /// <summary>
    ///     Returns whether the given object is a <see cref="TimezoneDateTime" /> representing the same instant.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is TimezoneDateTime tz)
            return Equals(this, tz);
        return false;
    }

    /// <summary>Returns whether the two values represent different instants.</summary>
    public static bool operator !=(TimezoneDateTime a, TimezoneDateTime b)
    {
        return !(a == b);
    }

    /// <summary>
    ///     Returns a hash code based on the represented instant, consistent with the equality semantics.
    /// </summary>
    public override int GetHashCode()
    {
        return Ticks.GetHashCode();
    }

    /// <summary>
    ///     System.Text.Json converter factory for <see cref="TimezoneDateTime" /> and its nullable form.
    /// </summary>
    public class TimezoneDateTimeJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(TimezoneDateTime) || typeToConvert == typeof(TimezoneDateTime?);
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(TimezoneDateTime))
                return new NonNullableJsonConverter();
            if (typeToConvert == typeof(TimezoneDateTime?))
                return new NullableJsonConverter();

            throw new NotSupportedException($"The type {typeToConvert} is not supported by {nameof(TimezoneDateTimeJsonConverterFactory)}");
        }

        /// <summary>
        ///     Reads an ISO 8601 string token as a UTC instant. The wire format carries only the UTC
        ///     instant; the timezone is not serialized, so values always deserialize with the UTC timezone.
        /// </summary>
        private static TimezoneDateTime ReadStringToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"The value is expected to be a string, but was {reader.TokenType}");

            // Fast path: ISO 8601 parsed directly from the UTF-8 payload, without allocating a string.
            if (reader.TryGetDateTime(out var dt))
            {
                dt = dt.Kind switch
                {
                    DateTimeKind.Utc => dt,
                    DateTimeKind.Local => dt.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                };
                return new TimezoneDateTime(dt, LocalTimezone.Utc);
            }

            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str) || !DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                throw new JsonException($"Invalid date format: \"{str}\"");

            return new TimezoneDateTime(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), LocalTimezone.Utc);
        }

        /// <summary>
        ///     Writes the UTC instant as an ISO 8601 string with a Z suffix; fractional seconds are
        ///     emitted only when present.
        /// </summary>
        private static void WriteUtcInstant(Utf8JsonWriter writer, TimezoneDateTime value)
        {
            writer.WriteStringValue(value.GetUtcDateTime());
        }

        /// <summary>
        ///     Converter for the non-nullable <see cref="TimezoneDateTime" />. A JSON null reads as
        ///     <see cref="Empty" />.
        /// </summary>
        public class NonNullableJsonConverter : JsonConverter<TimezoneDateTime>
        {
            /// <inheritdoc />
            public override TimezoneDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return Empty;

                return ReadStringToken(ref reader);
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, TimezoneDateTime value, JsonSerializerOptions options)
            {
                WriteUtcInstant(writer, value);
            }
        }

        /// <summary>
        ///     Converter for the nullable <see cref="TimezoneDateTime" />. A JSON null reads as null.
        /// </summary>
        public class NullableJsonConverter : JsonConverter<TimezoneDateTime?>
        {
            /// <inheritdoc />
            public override TimezoneDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                return ReadStringToken(ref reader);
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, TimezoneDateTime? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }

                WriteUtcInstant(writer, value.Value);
            }
        }
    }
}