namespace HelpMotivateMe.Core.DTOs.Admin;

public record UserActivityResponse(
    Guid UserId,
    string Username,
    string Email,
    
    // Last week stats
    UserActivityPeriod LastWeek,
    
    // Total (all time) stats
    UserActivityPeriod Total
);

public record UserActivityPeriod(
    int TasksCreated,
    int TasksCompleted,
    int GoalsCreated,
    int IdentitiesCreated,
    int HabitStacksCreated,
    int HabitCompletions,
    int JournalEntries,
    int AiCalls,
    decimal AiCostUsd
);
