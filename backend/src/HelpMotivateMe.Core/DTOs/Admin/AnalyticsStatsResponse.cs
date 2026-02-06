namespace HelpMotivateMe.Core.DTOs.Admin;

public record AnalyticsOverviewResponse(
    int TotalEvents,
    int UniqueUsers,
    int UniqueSessions,
    double AvgEventsPerSession,
    IEnumerable<EventTypeCount> TopEventTypes,
    IEnumerable<DailyEventCount> DailyEventCounts,
    IEnumerable<SessionSummary> RecentSessions
);

public record EventTypeCount(
    string EventType,
    int Count
);

public record DailyEventCount(
    string Date,
    int Count
);

public record SessionSummary(
    Guid SessionId,
    Guid UserId,
    string Email,
    DateTime FirstEvent,
    DateTime LastEvent,
    int EventCount,
    double DurationMinutes
);
