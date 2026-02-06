namespace HelpMotivateMe.Core.DTOs.Analytics;

public record TaskStreakResponse(
    Guid TaskId,
    string TaskTitle,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastCompletedDate,
    bool IsOnGracePeriod,
    int DaysUntilStreakBreaks
);

public record StreakSummaryResponse(
    int TotalHabits,
    int ActiveStreaks,
    int LongestActiveStreak,
    IEnumerable<TaskStreakResponse> Streaks
);

public record CompletionRateResponse(
    double DailyRate,
    double WeeklyRate,
    double MonthlyRate,
    int TotalCompletions,
    int MissedDays
);