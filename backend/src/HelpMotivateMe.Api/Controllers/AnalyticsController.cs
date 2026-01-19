using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Analytics;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private const string SessionIdKey = "AnalyticsSessionId";
    private readonly AppDbContext _db;
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(AppDbContext db, IAnalyticsService analyticsService)
    {
        _db = db;
        _analyticsService = analyticsService;
    }

    [HttpGet("streaks")]
    public async Task<ActionResult<StreakSummaryResponse>> GetAllStreaks()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "AnalyticsPageLoaded");

        // Get all tasks
        var tasks = await _db.TaskItems
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

        // Get all tasks for this user
        var tasks = await _db.TaskItems
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

        // Without TaskCompletion, we can show tasks completed by their CompletedAt date
        var completedTasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Where(t => t.Goal.UserId == userId &&
                        t.Status == TaskItemStatus.Completed &&
                        t.CompletedAt.HasValue &&
                        t.CompletedAt.Value >= startDate)
            .ToListAsync();

        var heatmapData = completedTasks
            .Where(t => t.CompletedAt.HasValue)
            .GroupBy(t => t.CompletedAt!.Value)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(heatmapData);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private Guid GetSessionId()
    {
        var sessionIdString = HttpContext.Session.GetString(SessionIdKey);
        if (sessionIdString != null && Guid.TryParse(sessionIdString, out var sessionId))
        {
            return sessionId;
        }

        var newSessionId = Guid.NewGuid();
        HttpContext.Session.SetString(SessionIdKey, newSessionId.ToString());
        return newSessionId;
    }
}
