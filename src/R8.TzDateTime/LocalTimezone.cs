using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using R8.TzDateTime.TimezoneMappers;

namespace R8.TzDateTime;

/// <summary>
///     A configured timezone: its IANA ids, culture, calendar, and clock. Instances are flyweights resolved
///     from a fixed registry (see <see cref="GetTimezone(string?)" />); the ambient one is
///     <see cref="Current" />. Equality and comparison are by canonical IANA id and current UTC offset.
/// </summary>
[DebuggerDisplay("{" + nameof(DefaultIanaId) + "}")]
public sealed class LocalTimezone : ITimezone, IEquatable<LocalTimezone>, IComparable<LocalTimezone>, IFormattable
{
    private static readonly ConcurrentDictionary<string, LocalTimezoneOptions> _options = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, LocalTimezone> _instances = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<ushort, LocalTimezoneOptions> _indices = new();

    // Index → timezone lookup for lock-free resolution on hot paths. Grown by copy-and-publish under
    // _registrationLock when a timezone is added at runtime; the volatile reference makes each published
    // array visible to readers. Existing indices never move, so a reader always resolves them correctly
    // against whichever array snapshot it observes.
    private static volatile LocalTimezone[] _byIndex = Array.Empty<LocalTimezone>();

    // Serializes runtime registrations (AddTimezone). Reads stay lock-free.
    private static readonly object _registrationLock = new();

    // The next free index; advanced as timezones are registered.
    private static ushort _nextIndex;

    private static volatile bool _initialized;

    private static readonly AsyncLocal<LocalTimezone?> _local = new();
    private static readonly object _localLock = new();

    private static volatile LocalTimezone? _current;
    private bool? _isRTL;

    internal static ushort UtcIndex;

    private LocalTimezoneOptions _currentOptions = null!;

    static LocalTimezone()
    {
        InitializeUnsafe();

        // UTC is the only built-in timezone. If the system zone hasn't been registered (via AddTimezone),
        // fall back to UTC rather than throwing — the ambient timezone must always resolve.
        var systemDefaultTimezone = GetSystemTimezone();
        _current ??= _local.Value ?? (TryGetTimezone(systemDefaultTimezone.Id, out var systemTimezone) ? systemTimezone : Utc);
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="LocalTimezone" />.
    /// </summary>
    private LocalTimezone()
    {
    }

    /// <summary>
    ///     Gets or sets the current timezone.
    /// </summary>
    /// <remarks>If a scope is started using <see cref="StartScope" />, this property will return the timezone of the scope.</remarks>
    public static LocalTimezone Current
    {
        get => _local.Value ?? _current ?? InitializeCurrentSlow();
        set => _current = value;
    }

    private static LocalTimezone InitializeCurrentSlow()
    {
        lock (_localLock)
        {
            return _current ??= TryGetTimezone(GetSystemTimezone().Id, out var systemTimezone) ? systemTimezone : Utc;
        }
    }

    /// <summary>
    ///     Gets all configured timezones (one per registered options entry).
    /// </summary>
    public static IReadOnlyCollection<LocalTimezone> Timezones => _options.Values.Select(x => GetTimezone(x.DefaultIanaId)).ToArray();

    /// <summary>Gets the UTC timezone — the only built-in timezone; all others are added via <see cref="AddTimezone(string, CultureInfo, CalendarSystem, string[])" />.</summary>
    public static LocalTimezone Utc => GetTimezone(UtcTimezone.DefaultId);

    /// <summary>Gets the canonical IANA id of this timezone (the first configured id).</summary>
    public string DefaultIanaId => IanaIds is { Length: > 0 } ? IanaIds[0] : string.Empty;

    /// <summary>Gets the IANA ids this timezone answers to, canonical id first.</summary>
    public string[] IanaIds { get; private init; } = null!;

    /// <summary>Gets the days of the week ordered by the culture's first day of week.</summary>
    public DayOfWeek[] DaysOfWeek { get; private init; } = null!; // Since the options.DaysOfWeek is a computed property, we need to set it here.

    /// <summary>Gets the culture associated with this timezone.</summary>
    public CultureInfo Culture => _currentOptions.Culture;

    /// <summary>Gets the zoned clock (zone + calendar) of this timezone.</summary>
    public ZonedClock Clock => _currentOptions.Clock;

    /// <summary>Gets the current UTC offset of this timezone (evaluated at the current instant).</summary>
    public Offset Offset => _currentOptions.Clock.GetCurrentOffsetDateTime().Offset;

    /// <summary>Gets the calendar system of this timezone (e.g. Persian for Asia/Tehran).</summary>
    public CalendarSystem Calendar => _currentOptions.Clock.Calendar;

    /// <summary>
    ///     Gets the stable index of this timezone, shared by all of its IANA aliases.
    /// </summary>
    internal ushort Index => _currentOptions._index;

    /// <summary>
    ///     Gets the start (unix ticks) of the zone's final tzdb interval; instants at or after it use a
    ///     constant offset. <see cref="long.MaxValue" /> when the fast path is disabled.
    /// </summary>
    internal long FinalIntervalStartUnixTicks => _currentOptions._finalIntervalStartUnixTicks;

    /// <summary>
    ///     Gets the constant UTC offset (ticks) of the zone's final tzdb interval.
    /// </summary>
    internal long FinalIntervalOffsetTicks => _currentOptions._finalIntervalOffsetTicks;

    /// <summary>
    ///     Gets the wall-clock threshold (unix ticks) from which local times are guaranteed unambiguous
    ///     inside the final interval — past the last transition and its ambiguity window.
    /// </summary>
    internal long FinalIntervalSafeWallUnixTicks => _currentOptions._finalIntervalSafeWallUnixTicks;

    /// <summary>
    ///     Gets whether the calendar is ISO/Gregorian, allowing calendar arithmetic to use BCL
    ///     <see cref="DateTime" /> math instead of NodaTime.
    /// </summary>
    internal bool UsesGregorianCalendar => _currentOptions._usesGregorianCalendar;

    /// <summary>
    /// Gets a value indicating whether the current culture is right-to-left (RTL).
    /// </summary>
    public bool IsRTL => _isRTL ?? (_isRTL = _currentOptions.Culture.TextInfo.IsRightToLeft).Value;

    /// <summary>
    ///     Gets the ID of the timezone
    /// </summary>
    public TimeZoneInfo GetSystemTimeZone()
    {
        return TimeZoneInfo.FindSystemTimeZoneById(DefaultIanaId);
    }

    /// <summary>
    ///     Returns the number of the specified day of the week in the current timezone.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dayOfWeek" /> is not a valid day of the week.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetDayNumberInWeek(DayOfWeek dayOfWeek)
    {
        for (var i = 0; i < DaysOfWeek.Length; i++)
            if (DaysOfWeek[i] == dayOfWeek)
                return i;

        throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Invalid day of week");
    }

    private static void InitializeUnsafe()
    {
        if (_initialized)
            return;

        _instances.Clear();
        _indices.Clear();

        // UTC is the only built-in timezone, registered first so it keeps index 0 — default(TimezoneDateTime),
        // JSON null, and the UTC fast path all depend on that. Every other timezone is added at runtime via
        // AddTimezone. Registered under _registrationLock so it composes with concurrent AddTimezone calls.
        lock (_registrationLock)
        {
            ushort index = 0;
            RegisterUnsafe(new UtcTimezone(), ref index);

            var byIndex = new LocalTimezone[index];
            foreach (var pair in _indices)
                byIndex[pair.Key] = GetTimezone(pair.Value.DefaultIanaId);
            foreach (var ianaId in _options.Keys)
                _ = GetTimezone(ianaId);

            _byIndex = byIndex;
            _nextIndex = index;
            _initialized = true;
        }
    }

    /// <summary>
    ///     Registers a timezone from its IANA id, culture and calendar. The zone becomes resolvable via
    ///     <see cref="GetTimezone(string?)" /> by its id or any alias, and usable with <see cref="TimezoneDateTime" />.
    ///     Idempotent: registering an already-known id returns the existing timezone. Thread-safe.
    /// </summary>
    /// <param name="ianaId">The canonical IANA id (e.g. "Asia/Tehran"). Must be a valid tzdb id.</param>
    /// <param name="culture">The culture that drives formatting, first-day-of-week and RTL.</param>
    /// <param name="calendar">The calendar system to express date components in (e.g. <see cref="CalendarSystem.PersianSimple" />).</param>
    /// <param name="aliases">Optional additional ids/links the zone also answers to (e.g. "Iran").</param>
    /// <returns>The registered (or existing) <see cref="LocalTimezone" />.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public static LocalTimezone AddTimezone(string ianaId, CultureInfo culture, CalendarSystem calendar, params string[] aliases)
    {
        ArgumentNullException.ThrowIfNull(ianaId);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(calendar);

        var ids = aliases is { Length: > 0 }
            ? new[] { ianaId }.Concat(aliases).ToArray()
            : new[] { ianaId };
        return AddTimezone(new MappedTimezone(ids, culture, calendar));
    }

    /// <summary>
    ///     Registers a timezone described by a <see cref="LocalTimezoneOptions" /> subclass.
    /// </summary>
    /// <typeparam name="TOptions">A parameterless <see cref="LocalTimezoneOptions" /> subclass.</typeparam>
    /// <returns>The registered (or existing) <see cref="LocalTimezone" />.</returns>
    public static LocalTimezone AddTimezone<TOptions>() where TOptions : LocalTimezoneOptions, new()
    {
        return AddTimezone(new TOptions());
    }

    /// <summary>
    ///     Registers a timezone from an explicit <see cref="LocalTimezoneOptions" /> instance. Idempotent and
    ///     thread-safe: registering an already-known canonical id returns the existing timezone.
    /// </summary>
    /// <param name="options">The timezone options (IANA ids, culture, calendar).</param>
    /// <returns>The registered (or existing) <see cref="LocalTimezone" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="options" /> declares no IANA id.</exception>
    /// <exception cref="InvalidOperationException">The timezone registry is full (65 535 zones).</exception>
    public static LocalTimezone AddTimezone(LocalTimezoneOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.IanaIds is not { Length: > 0 })
            throw new ArgumentException("The timezone options must declare at least one IANA id.", nameof(options));

        lock (_registrationLock)
        {
            // Already registered → return the existing flyweight (idempotent).
            if (_options.ContainsKey(options.DefaultIanaId))
                return GetTimezone(options.DefaultIanaId);

            if (_nextIndex == ushort.MaxValue)
                throw new InvalidOperationException("The timezone registry is full.");

            var index = _nextIndex;
            PrimeOptions(options, index); // validates the tzdb id and primes the fast-path caches

            // Build the flyweight inline — GetTimezone can't be used yet (it resolves via _options).
            var timezone = new LocalTimezone
            {
                IanaIds = options.IanaIds,
                DaysOfWeek = options.DaysOfWeek,
                _currentOptions = options,
            };

            // Publish in an order that is safe for lock-free readers: grow and publish the index table
            // BEFORE the id becomes resolvable through _options. Otherwise a thread could resolve the new
            // id, read its index, and hit GetTimezoneByIndex against a _byIndex that hasn't grown yet.
            var grown = new LocalTimezone[index + 1];
            Array.Copy(_byIndex, grown, _byIndex.Length);
            grown[index] = timezone;
            _byIndex = grown; // volatile publish

            _indices[index] = options;
            foreach (var id in options.IanaIds)
                _instances.TryAdd(id, timezone);
            foreach (var id in options.IanaIds)
                _options.TryAdd(id, options); // visibility gate — must be last

            _nextIndex = (ushort)(index + 1);
            return timezone;
        }
    }

    private static void RegisterUnsafe(LocalTimezoneOptions options, ref ushort index)
    {
        var registered = false;
        for (var i = 0; i < options.IanaIds.Length; i++)
            registered |= _options.TryAdd(options.IanaIds[i], options);

        if (!registered)
            return;

        PrimeOptions(options, index);
        _indices.AddOrUpdate(index, options, (_, _) => options);

        if (options.DefaultIanaId.Equals(UtcTimezone.DefaultId, StringComparison.Ordinal))
        {
            UtcIndex = index;
        }

        index++;
    }

    /// <summary>
    ///     Assigns the index and primes the clock and cached fast-path fields on the options. All IANA
    ///     aliases of a zone share this one options instance and index.
    /// </summary>
    private static void PrimeOptions(LocalTimezoneOptions options, ushort index)
    {
        options._index = index;
        options.Clock = SystemClock.Instance.InZone(DateTimeZoneProviders.Tzdb[options.DefaultIanaId], options.Calendar);
        options._usesGregorianCalendar = options.Calendar == CalendarSystem.Iso || options.Calendar == CalendarSystem.Gregorian;
        CacheFinalIntervalUnsafe(options);
    }

    /// <summary>
    ///     Probes the zone's final tzdb interval and caches its start and offset so date math on
    ///     instants past the last transition can skip zone-interval lookups entirely.
    /// </summary>
    private static void CacheFinalIntervalUnsafe(LocalTimezoneOptions options)
    {
        var zone = options.Clock.Zone;
        var finalProbe = zone.GetZoneInterval(Instant.FromUtc(5000, 1, 1, 0, 0));
        if (finalProbe.HasEnd)
            return; // transitions continue indefinitely; the fast path stays disabled

        var offsetTicks = finalProbe.WallOffset.Ticks;
        options._finalIntervalOffsetTicks = offsetTicks;
        if (finalProbe.HasStart)
        {
            var startUnixTicks = finalProbe.Start.ToUnixTimeTicks();
            var previousOffsetTicks = zone.GetZoneInterval(finalProbe.Start.Minus(Duration.Epsilon)).WallOffset.Ticks;
            options._finalIntervalStartUnixTicks = startUnixTicks;
            options._finalIntervalSafeWallUnixTicks = startUnixTicks + Math.Max(offsetTicks, previousOffsetTicks);
        }
        else
        {
            options._finalIntervalStartUnixTicks = long.MinValue;
            options._finalIntervalSafeWallUnixTicks = long.MinValue;
        }
    }

    /// <summary>
    ///     Attempts to get a <see cref="LocalTimezoneOptions"/> object associated with the specified IANA ID.
    /// </summary>
    public static bool TryGetTimezone(string ianaId, [NotNullWhen(true)] out LocalTimezone? value)
    {
        if (_options.TryGetValue(ianaId, out var options))
        {
            value = GetTimezone(options.DefaultIanaId);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="LocalTimezone" />.
    /// </summary>
    /// <param name="ianaId">A valid IANA ID.</param>
    /// <returns>A <see cref="LocalTimezone" /> object</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ianaId" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the timezone is not configured.</exception>
    public static LocalTimezone GetTimezone(string? ianaId)
    {
        ArgumentNullException.ThrowIfNull(ianaId);
        if (_instances.TryGetValue(ianaId, out var timezone))
            return timezone;

        if (!_options.TryGetValue(ianaId, out var options))
            throw new InvalidOperationException($"The timezone '{ianaId}' was not configured.");

        timezone = new LocalTimezone
        {
            IanaIds = options.IanaIds,
            DaysOfWeek = options.DaysOfWeek,
            _currentOptions = options,
        };
        if (!_instances.TryAdd(ianaId, timezone))
            return _instances[ianaId];

        return timezone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static LocalTimezone GetTimezoneByIndex(ushort index)
    {
        var byIndex = _byIndex;
        if (index < byIndex.Length)
            return byIndex[index];

        throw new KeyNotFoundException($"The timezone with index '{index}' was not found.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort GetTimezoneIndex(string ianaId)
    {
        if (_options.TryGetValue(ianaId, out var timezone))
            return timezone._index;

        throw new KeyNotFoundException($"The timezone '{ianaId}' was not found.");
    }

    /// <summary>
    ///     Starts a new scope for the current timezone. This will set the current timezone to the specified timezone. This
    ///     method is useful when you want to change the timezone for a specific scope (ASP.NET Core).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timezone" /> is null.</exception>
    /// <remarks>
    ///     Don't forget to call <see cref="EndScope" /> to end the scope, when you are done with the timezone. Otherwise,
    ///     the timezone will remain the same.
    /// </remarks>
    public static void StartScope(LocalTimezone timezone)
    {
        if (timezone == null)
            throw new ArgumentNullException(nameof(timezone));

        _local.Value = timezone;
    }

    /// <summary>
    ///     Ends the current timezone scope. This will set the current timezone to default.
    /// </summary>
    public static void EndScope()
    {
        _local.Value = null;
    }

    private static DateTimeZone GetSystemTimezone() => DateTimeZoneProviders.Tzdb.GetSystemDefault();

    /// <summary>
    ///     Returns a list of all configured timezones known to the tzdb source. Unconfigured tzdb ids are
    ///     skipped instead of throwing, since only configured zones can produce a <see cref="LocalTimezone" />.
    /// </summary>
    /// <returns>A enumeration of all timezones</returns>
    /// <exception cref="TimeZoneNotFoundException">When the timezone source is null</exception>
    public static IEnumerable<LocalTimezone> GetTimezones()
    {
        var source = TzdbDateTimeZoneSource.Default;
        if (source == null)
            throw new TimeZoneNotFoundException(nameof(source));

        return source.TzdbToWindowsIds
            .Where(x => _options.ContainsKey(x.Key))
            .Select(x => GetTimezone(x.Key));
    }

    /// <summary>
    ///     Returns a list of all timezones ordered by their offset
    /// </summary>
    /// <returns>An array of timezones</returns>
    public static GroupedTimezones[] GetTimezonesGroupedByOffset(bool onlyMappedTimezones = false)
    {
        IEnumerable<LocalTimezone> timezones;
        if (onlyMappedTimezones)
            timezones = _options
                .Select(x => GetTimezone(x.Value.DefaultIanaId));
        else
            timezones = GetTimezones()
                .Where(timezone => timezone.DefaultIanaId.StartsWith("America", StringComparison.Ordinal) ||
                                   timezone.DefaultIanaId.StartsWith("Europe", StringComparison.Ordinal) ||
                                   timezone.DefaultIanaId.StartsWith("Asia", StringComparison.Ordinal) ||
                                   timezone.DefaultIanaId.StartsWith("Africa", StringComparison.Ordinal) ||
                                   timezone.DefaultIanaId.StartsWith("Australia", StringComparison.Ordinal) ||
                                   timezone.DefaultIanaId.StartsWith("Pacific", StringComparison.Ordinal));

        var output = timezones
            .OrderBy(x => x.Offset)
            .GroupBy(timezone => timezone.Offset)
            .Select(groupedTimezones =>
            {
                var offset = groupedTimezones.Key;
                var timezones = groupedTimezones
                    .Select(timezone => new
                    {
                        DisplayName = (timezone.DefaultIanaId.Contains('/')
                            ? timezone.DefaultIanaId.Split('/')[1]
                            : timezone.DefaultIanaId).Replace("_", " "),
                        Timezone = timezone
                    })
                    .DistinctBy(timezone => timezone.Timezone.DefaultIanaId)
                    .ToArray();

                var ianaIds = timezones
                    .ToDictionary(tuple => tuple.Timezone.DefaultIanaId, tuple => new GroupedTimezones.Timezone
                    {
                        DisplayName = tuple.DisplayName,
                        IsActive = tuple.Timezone == Current,
                        FullySupported = _options.TryGetValue(tuple.Timezone.DefaultIanaId, out var map) && map != null
                    }, StringComparer.Ordinal);

                return new GroupedTimezones
                {
                    Offset = offset == Offset.Zero ? "00:00" : offset.ToString("m", CultureInfo.CurrentCulture),
                    IanaIds = ianaIds
                };
            })
            .ToArray();

        if (!output.Any(x => x.IanaIds.Any(c => c.Value.IsActive)))
        {
            // Best-effort default: mark a zero-offset entry active when the current timezone is absent.
            var group = output.FirstOrDefault(x => x.Offset is "00:00" or "+00:00");
            var utc = group?.IanaIds.FirstOrDefault(x => x.Value.DisplayName.Contains("UTC", StringComparison.Ordinal) || x.Value.DisplayName.Contains("London", StringComparison.Ordinal)).Value;
            if (utc != null)
                utc.IsActive = true;
        }

        return output;
    }

    /// <summary>
    ///     Returns the "GMT±hh:mm" representation of this timezone (or the IANA id before initialization).
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (_currentOptions is { Clock: not null })
        {
            sb.Append("GMT");
            sb.Append(Offset.ToString("m", CultureInfo.CurrentCulture));
        }
        else
        {
            sb.Append(DefaultIanaId);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Formats this timezone: "G" = GMT offset, "N" = IANA id, "O" = offset, "C" = culture name,
    ///     "A" = calendar id, "F" = first day of week.
    /// </summary>
    /// <exception cref="FormatException">Thrown for unknown format strings.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (format == null)
            return ToString();

        if (format.Equals("G", StringComparison.OrdinalIgnoreCase))
            return ToString();

        if (format.Equals("N", StringComparison.OrdinalIgnoreCase))
            return DefaultIanaId;

        if (format.Equals("O", StringComparison.OrdinalIgnoreCase))
            return Offset.ToString("m", formatProvider ?? Culture);

        if (format.Equals("C", StringComparison.OrdinalIgnoreCase))
            return Culture.Name;

        if (format.Equals("A", StringComparison.OrdinalIgnoreCase))
            return Calendar.Id;

        if (format.Equals("F", StringComparison.OrdinalIgnoreCase))
            return Culture.DateTimeFormat.FirstDayOfWeek.ToString();

        throw new FormatException("Invalid format string");
    }

    /// <summary>Returns whether the other timezone has the same canonical IANA id.</summary>
    public bool Equals(ITimezone? other)
    {
        if (other is null)
            return false;
        return DefaultIanaId.Equals(other.DefaultIanaId, StringComparison.Ordinal);
    }

    /// <summary>Null-safe equality by canonical IANA id.</summary>
    public static bool Equal(LocalTimezone? left, LocalTimezone? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    /// <summary>Returns whether the given object is a timezone with the same canonical IANA id.</summary>
    public override bool Equals(object? obj)
    {
        return obj is LocalTimezone other && Equals(other);
    }

    /// <summary>Returns whether the other timezone has the same canonical IANA id.</summary>
    public bool Equals(LocalTimezone? other)
    {
        if (other is null)
            return false;
        return DefaultIanaId.Equals(other.DefaultIanaId, StringComparison.Ordinal);
    }

    /// <summary>Null-safe comparison by current UTC offset.</summary>
    public static int CompareTo(LocalTimezone? left, LocalTimezone? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null) return -1;
        if (right is null) return 1;
        return left.CompareTo(right);
    }

    /// <summary>Compares timezones by their current UTC offset.</summary>
    public int CompareTo(LocalTimezone? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Offset.CompareTo(other.Offset);
    }

    /// <summary>Returns a hash code based on the canonical IANA id, consistent with equality.</summary>
    public override int GetHashCode()
    {
        return DefaultIanaId.GetHashCode(StringComparison.Ordinal);
    }

    /// <summary>Resolves an IANA id string to its configured timezone.</summary>
    public static implicit operator LocalTimezone(string? ianaId)
    {
        return GetTimezone(ianaId);
    }

    /// <summary>Converts a timezone to its canonical IANA id.</summary>
    public static implicit operator string(LocalTimezone timezone)
    {
        return timezone.DefaultIanaId;
    }

    /// <summary>Equality by canonical IANA id.</summary>
    public static bool operator ==(LocalTimezone? left, LocalTimezone? right)
    {
        return Equal(left, right);
    }

    /// <summary>Inequality by canonical IANA id.</summary>
    public static bool operator !=(LocalTimezone? left, LocalTimezone? right)
    {
        return !Equal(left, right);
    }

    /// <summary>Returns whether the left timezone has a smaller current UTC offset.</summary>
    public static bool operator <(LocalTimezone? left, LocalTimezone? right)
    {
        return CompareTo(left, right) < 0;
    }

    /// <summary>Returns whether the left timezone has a larger current UTC offset.</summary>
    public static bool operator >(LocalTimezone? left, LocalTimezone? right)
    {
        return CompareTo(left, right) > 0;
    }

    /// <summary>Returns whether the left timezone's current UTC offset is smaller or equal.</summary>
    public static bool operator <=(LocalTimezone? left, LocalTimezone? right)
    {
        return CompareTo(left, right) <= 0;
    }

    /// <summary>Returns whether the left timezone's current UTC offset is larger or equal.</summary>
    public static bool operator >=(LocalTimezone? left, LocalTimezone? right)
    {
        return CompareTo(left, right) >= 0;
    }
}