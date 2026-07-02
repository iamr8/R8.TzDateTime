namespace R8.TzDateTime;

/// <summary>
///     A set of timezones that share the same UTC offset, as returned by
///     <see cref="LocalTimezone.GetTimezonesGroupedByOffset" />.
/// </summary>
public class GroupedTimezones
{
    /// <summary>Gets the shared UTC offset of this group, formatted as "±hh:mm".</summary>
    public string Offset { get; init; } = null!;

    /// <summary>Gets the timezones in this group, keyed by their canonical IANA id.</summary>
    public IReadOnlyDictionary<string, Timezone> IanaIds { get; init; } = null!;

    /// <summary>
    ///     A single timezone entry within a <see cref="GroupedTimezones" />.
    /// </summary>
    public class Timezone
    {
        /// <summary>Gets the human-readable display name (the IANA id's last segment, underscores replaced with spaces).</summary>
        public string DisplayName { get; init; } = null!;

        /// <summary>Gets or sets whether this timezone is the current one (<see cref="LocalTimezone.Current" />).</summary>
        public bool IsActive { get; set; }

        /// <summary>Gets whether this timezone has dedicated configured options (as opposed to being tzdb-only).</summary>
        public bool FullySupported { get; init; }
    }
}
