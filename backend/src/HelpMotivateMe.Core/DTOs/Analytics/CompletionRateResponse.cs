namespace HelpMotivateMe.Core.DTOs.Analytics;

public record CompletionRateResponse(
    double DailyRate,
    double WeeklyRate,
    double MonthlyRate,
    int TotalCompletions,
    int MissedDays
);
