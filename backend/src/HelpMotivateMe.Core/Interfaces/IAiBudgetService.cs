namespace HelpMotivateMe.Core.Interfaces;

public interface IAiBudgetService
{
    Task<BudgetCheckResult> CheckBudgetAsync(Guid userId, decimal estimatedCost,
        CancellationToken cancellationToken = default);

    Task<AiBudgetStats> GetBudgetStatsAsync(CancellationToken cancellationToken = default);
}

public record BudgetCheckResult(bool IsAllowed, string? DenialReason = null);

public record AiBudgetStats(
    decimal TotalEstimatedAllTime,
    decimal TotalActualAllTime,
    decimal TotalEstimatedLast30Days,
    decimal TotalActualLast30Days,
    decimal GlobalLimitLast30DaysUsd,
    decimal PerUserLimitLast30DaysUsd
);