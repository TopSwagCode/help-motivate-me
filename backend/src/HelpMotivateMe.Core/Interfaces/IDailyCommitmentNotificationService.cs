namespace HelpMotivateMe.Core.Interfaces;

/// <summary>
/// Service for processing daily identity commitment push notifications.
/// Scans eligible users and sends at most one notification per time slot per day.
/// </summary>
public interface IDailyCommitmentNotificationService
{
    /// <summary>
    /// Processes all eligible users and sends daily commitment notifications.
    /// This is the single entry point called by the periodic background worker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>The number of notifications successfully sent.</returns>
    Task<int> ProcessEligibleUsersAsync(CancellationToken cancellationToken = default);
}
