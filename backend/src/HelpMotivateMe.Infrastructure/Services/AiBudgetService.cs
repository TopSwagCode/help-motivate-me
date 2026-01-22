using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelpMotivateMe.Infrastructure.Services;

public class AiBudgetService : IAiBudgetService
{
    private readonly AppDbContext _db;
    private readonly AiBudgetOptions _options;
    private readonly ILogger<AiBudgetService> _logger;

    public AiBudgetService(
        AppDbContext db,
        IOptions<AiBudgetOptions> options,
        ILogger<AiBudgetService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(
        Guid userId,
        decimal estimatedCost,
        CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Check global budget (actual cost only)
        var globalActualCostLast30Days = await _db.AiUsageLogs
            .Where(l => l.CreatedAt >= thirtyDaysAgo)
            .SumAsync(l => l.ActualCostUsd, cancellationToken);

        if (globalActualCostLast30Days >= _options.GlobalLimitLast30DaysUsd)
        {
            _logger.LogWarning(
                "Global AI budget exceeded. Current: ${Current:F4}, Limit: ${Limit:F4}",
                globalActualCostLast30Days, _options.GlobalLimitLast30DaysUsd);
            return new BudgetCheckResult(false, "Global AI budget limit has been reached. Please try again later.");
        }

        // Check per-user budget (use max of estimated and actual for better accuracy)
        var userCostLast30Days = await _db.AiUsageLogs
            .Where(l => l.UserId == userId && l.CreatedAt >= thirtyDaysAgo)
            .SumAsync(l => l.EstimatedCostUsd > l.ActualCostUsd ? l.EstimatedCostUsd : l.ActualCostUsd, cancellationToken);

        var projectedUserCost = userCostLast30Days + estimatedCost;
        if (projectedUserCost > _options.PerUserLimitLast30DaysUsd)
        {
            _logger.LogWarning(
                "Per-user AI budget exceeded for user {UserId}. Current: ${Current:F4}, Projected: ${Projected:F4}, Limit: ${Limit:F4}",
                userId, userCostLast30Days, projectedUserCost, _options.PerUserLimitLast30DaysUsd);
            return new BudgetCheckResult(false, "Your personal AI usage limit has been reached. Please try again later.");
        }

        return new BudgetCheckResult(true);
    }

    public async Task<AiBudgetStats> GetBudgetStatsAsync(CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // All-time totals
        var allTimeTotals = await _db.AiUsageLogs
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEstimated = g.Sum(l => l.EstimatedCostUsd),
                TotalActual = g.Sum(l => l.ActualCostUsd)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Last 30 days totals
        var last30DaysTotals = await _db.AiUsageLogs
            .Where(l => l.CreatedAt >= thirtyDaysAgo)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEstimated = g.Sum(l => l.EstimatedCostUsd),
                TotalActual = g.Sum(l => l.ActualCostUsd)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new AiBudgetStats(
            TotalEstimatedAllTime: allTimeTotals?.TotalEstimated ?? 0m,
            TotalActualAllTime: allTimeTotals?.TotalActual ?? 0m,
            TotalEstimatedLast30Days: last30DaysTotals?.TotalEstimated ?? 0m,
            TotalActualLast30Days: last30DaysTotals?.TotalActual ?? 0m,
            GlobalLimitLast30DaysUsd: _options.GlobalLimitLast30DaysUsd,
            PerUserLimitLast30DaysUsd: _options.PerUserLimitLast30DaysUsd
        );
    }
}
