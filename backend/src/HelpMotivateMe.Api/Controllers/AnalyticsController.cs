using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Analytics;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/analytics")]
public class AnalyticsController : ApiControllerBase
{
    private readonly IQueryInterface<TaskItem> _tasks;
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IQueryInterface<TaskItem> tasks, IAnalyticsService analyticsService)
    {
        _tasks = tasks;
        _analyticsService = analyticsService;
    }

    [HttpGet("streaks")]
    public async Task<ActionResult<StreakSummaryResponse>> GetAllStreaks()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "AnalyticsPageLoaded");

        // Get all tasks using read-only query interface
        var tasks = await _tasks
            .Include(t => t.Goal)
            .Where(t => t.Goal.UserId == userId)
            .ToListAsync();

        // Without TaskCompletion, we can only show basic stats
        var streaks = tasks.Select(task => new TaskStreakResponse(
            task.Id,
            task.Title,
            0, // No streak tracking without completions
            0,
            task.CompletedAt,
            false,
            0
        )).ToList();

        return Ok(new StreakSummaryResponse(
            streaks.Count,
            0,
            0,
            streaks
        ));
    }

    [HttpGet("completion-rates")]
    public async Task<ActionResult<CompletionRateResponse>> GetCompletionRates()
    {
        var userId = GetUserId();

        // Get all tasks for this user using read-only query interface
        var tasks = await _tasks
            .Include(t => t.Goal)
            .Where(t => t.Goal.UserId == userId)
            .ToListAsync();

        if (tasks.Count == 0)
        {
            return Ok(new CompletionRateResponse(0, 0, 0, 0, 0));
        }

        var completedCount = tasks.Count(t => t.Status == TaskItemStatus.Completed);
        var completionRate = (double)completedCount / tasks.Count * 100;

        return Ok(new CompletionRateResponse(
            Math.Round(completionRate, 1),
            Math.Round(completionRate, 1),
            Math.Round(completionRate, 1),
            completedCount,
            0
        ));
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<IEnumerable<object>>> GetHeatmapData([FromQuery] int days = 90)
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-days);

        // Group and count at database level for better performance
        var heatmapData = await _tasks
            .Where(t => t.Goal.UserId == userId &&
                        t.Status == TaskItemStatus.Completed &&
                        t.CompletedAt.HasValue &&
                        t.CompletedAt.Value >= startDate &&
                        t.CompletedAt.Value <= today)
            .GroupBy(t => t.CompletedAt!.Value)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(heatmapData);
    }
}
