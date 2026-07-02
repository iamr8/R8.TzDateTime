namespace R8.TzDateTime;

/// <summary>
///     An interface to manage time-traveling.
/// </summary>
public interface ITimeMachine
{
    /// <summary>
    ///     Returns the current time in the specified timezone.
    /// </summary>
    /// <param name="timezone">A <see cref="LocalTimezone" /> object.</param>
    /// <returns>A <see cref="TimezoneDateTime" /> object.</returns>
    /// <remarks>It's recommended to be used inside services, to have more control over the time.</remarks>
    TimezoneDateTime Now(LocalTimezone? timezone = null);

    /// <summary>
    ///     Returns the current UTC time.
    /// </summary>
    /// <returns>A <see cref="DateTime" /> object.</returns>
    /// <remarks>It's recommended to be used inside services, to have more control over the time.</remarks>
    DateTime UtcNow();

    /// <summary>
    /// Creates a cancellable task that completes after a specified time interval.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1) to wait indefinitely.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the time delay.</returns>
    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}