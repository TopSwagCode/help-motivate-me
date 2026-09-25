using HelpMotivateMe.Core.Interfaces;

namespace HelpMotivateMe.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    public Task LogEventAsync(Guid userId, Guid sessionId, string eventType, object? metadata = null)
    {
        return Task.CompletedTask;
    }
}