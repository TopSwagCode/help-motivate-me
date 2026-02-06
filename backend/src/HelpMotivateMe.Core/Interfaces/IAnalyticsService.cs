namespace HelpMotivateMe.Core.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Log an analytics event for the current user/session.
    /// </summary>
    Task LogEventAsync(Guid userId, Guid sessionId, string eventType, object? metadata = null);
}
