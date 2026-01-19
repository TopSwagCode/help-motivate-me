using System.Text.Json;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;

namespace HelpMotivateMe.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogEventAsync(Guid userId, Guid sessionId, string eventType, object? metadata = null)
    {
        var analyticsEvent = new AnalyticsEvent
        {
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.AnalyticsEvents.Add(analyticsEvent);
        await _db.SaveChangesAsync();
    }
}
