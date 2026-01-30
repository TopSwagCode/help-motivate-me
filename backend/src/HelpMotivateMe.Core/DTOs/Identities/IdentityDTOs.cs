namespace HelpMotivateMe.Core.DTOs.Identities;

public record CreateIdentityRequest(
    string Name,
    string? Description,
    string? Color,
    string? Icon
);

public record UpdateIdentityRequest(
    string Name,
    string? Description,
    string? Color,
    string? Icon
);

public record IdentityResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    string? Icon,
    int TotalTasks,
    int CompletedTasks,
    int TasksCompletedLast7Days,
    double CompletionRate,
    int TotalGoals,
    int CompletedGoals,
    int TotalProofs,
    int TotalDailyCommitments,
    int CompletedDailyCommitments,
    DateTime CreatedAt
);

public record IdentityStatsResponse(
    Guid Id,
    string Name,
    int TotalCompletions,
    int CurrentStreak,
    int WeeklyCompletions,
    string ReinforcementMessage
);
