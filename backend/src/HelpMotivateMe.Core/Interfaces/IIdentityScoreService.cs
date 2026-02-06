using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Interfaces;

public interface IIdentityScoreService
{
    Task<List<IdentityScoreResult>> CalculateScoresAsync(Guid userId, DateOnly targetDate);
}

public record IdentityScoreResult(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    IdentityStatus Status,
    TrendDirection Trend,
    int AccountAgeDays,
    bool ShowNumericScore,
    int TodayVotes
);