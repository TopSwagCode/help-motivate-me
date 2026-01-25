using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.DailyCommitment;
using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/today")]
public class TodayController : ControllerBase
{
    private const string SessionIdKey = "AnalyticsSessionId";
    private readonly IQueryInterface<HabitStack> _habitStacks;
    private readonly IQueryInterface<TaskItem> _tasks;
    private readonly IQueryInterface<Identity> _identities;
    private readonly IdentityScoreService _identityScoreService;
    private readonly DailyCommitmentService _commitmentService;
    private readonly IAnalyticsService _analyticsService;

    public TodayController(
        IQueryInterface<HabitStack> habitStacks,
        IQueryInterface<TaskItem> tasks,
        IQueryInterface<Identity> identities,
        IdentityScoreService identityScoreService,
        DailyCommitmentService commitmentService,
        IAnalyticsService analyticsService)
    {
        _habitStacks = habitStacks;
        _tasks = tasks;
        _identities = identities;
        _identityScoreService = identityScoreService;
        _commitmentService = commitmentService;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get the today view for a specific date (defaults to today).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TodayViewResponse>> GetTodayView([FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        await _analyticsService.LogEventAsync(userId, sessionId, "TodayPageLoaded", new { date = targetDate });

        // Get habit stacks with completions
        var habitStacks = await GetTodayHabitStacks(userId, targetDate);

        // Get upcoming tasks (pending)
        var upcomingTasks = await GetUpcomingTasks(userId, targetDate);

        // Get completed tasks for this date
        var completedTasks = await GetCompletedTasks(userId, targetDate);

        // Get identity feedback (based on habit stack item completions + task completions)
        var identityFeedback = await GetIdentityFeedback(userId, targetDate);

        // Get identity scores (progress bars)
        var identityScores = await _identityScoreService.CalculateScoresAsync(userId, targetDate);
        var identityProgress = identityScores.Select(s => new IdentityProgressResponse(
            s.Id,
            s.Name,
            s.Color,
            s.Icon,
            s.Score,
            s.Status.ToString(),
            s.Trend.ToString(),
            s.AccountAgeDays,
            s.ShowNumericScore
        )).ToList();

        // Get daily commitment for today
        var dailyCommitment = await _commitmentService.GetCommitmentAsync(userId, targetDate);

        // Get yesterday's commitment info (for recovery message)
        var yesterdayCommitment = await _commitmentService.GetYesterdayCommitmentAsync(userId);

        return Ok(new TodayViewResponse(
            targetDate,
            habitStacks,
            upcomingTasks,
            completedTasks,
            identityFeedback,
            identityProgress,
            dailyCommitment,
            yesterdayCommitment
        ));
    }

    private async Task<List<TodayHabitStackResponse>> GetTodayHabitStacks(Guid userId, DateOnly targetDate)
    {
        // Use filtered include to only load completions for the target date
        var stacks = await _habitStacks
            .Include(s => s.Identity)
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Completions.Where(c => c.CompletedDate == targetDate))
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return stacks.Select(s => new TodayHabitStackResponse(
            s.Id,
            s.Name,
            s.TriggerCue,
            s.IdentityId,
            s.Identity?.Name,
            s.Identity?.Color,
            s.Identity?.Icon,
            s.Items.Select(i => new TodayHabitStackItemResponse(
                i.Id,
                i.HabitDescription,
                i.Completions.Any(), // Already filtered to targetDate
                i.CurrentStreak
            )),
            s.Items.Count(i => i.Completions.Any()), // Already filtered to targetDate
            s.Items.Count
        )).ToList();
    }

    private async Task<List<TodayTaskResponse>> GetUpcomingTasks(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        // Include tasks with due date within 7 days OR tasks without any due date
        // Exclude completed tasks and tasks from completed goals
        var tasks = await _tasks
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status != TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow))
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)  // Tasks with due date first
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();

        return tasks.Select(t => new TodayTaskResponse(
            t.Id,
            t.Title,
            t.Description,
            t.GoalId,
            t.Goal.Title,
            t.IdentityId,
            t.Identity?.Name,
            t.Identity?.Icon,
            t.Identity?.Color,
            t.DueDate,
            t.Status.ToString()
        )).ToList();
    }

    private async Task<List<TodayTaskResponse>> GetCompletedTasks(Guid userId, DateOnly targetDate)
    {
        // Get tasks that are currently completed AND have due date within range (or no due date)
        // Exclude tasks from completed goals
        var weekFromNow = targetDate.AddDays(7);

        var tasks = await _tasks
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status == TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow) &&
                        t.CompletedAt == targetDate)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync();

        return tasks.Select(t => new TodayTaskResponse(
            t.Id,
            t.Title,
            t.Description,
            t.GoalId,
            t.Goal.Title,
            t.IdentityId,
            t.Identity?.Name,
            t.Identity?.Icon,
            t.Identity?.Color,
            t.DueDate,
            t.Status.ToString()
        )).ToList();
    }

    private async Task<List<TodayIdentityFeedbackResponse>> GetIdentityFeedback(Guid userId, DateOnly targetDate)
    {
        // Get identities with only today's completions (filtered at database level)
        var identities = await _identities
            .Include(i => i.HabitStacks)
                .ThenInclude(hs => hs.Items)
                    .ThenInclude(hsi => hsi.Completions.Where(c => c.CompletedDate == targetDate))
            .Include(i => i.Tasks.Where(t => t.Status == TaskItemStatus.Completed &&
                                             t.CompletedAt == targetDate))
            .Where(i => i.UserId == userId)
            .ToListAsync();

        return identities.Select(i =>
        {
            // Count habit stack item completions (already filtered to targetDate)
            var habitCompletionsToday = i.HabitStacks
                .SelectMany(hs => hs.Items)
                .SelectMany(hsi => hsi.Completions)
                .Count();

            // Count task completions (already filtered to targetDate)
            var taskCompletionsToday = i.Tasks.Count;

            var totalToday = habitCompletionsToday + taskCompletionsToday;

            return new TodayIdentityFeedbackResponse(
                i.Id,
                i.Name,
                i.Color,
                i.Icon,
                totalToday,
                GenerateReinforcementMessage(i.Name, totalToday)
            );
        })
        .Where(i => i.CompletionsToday > 0)
        .OrderByDescending(i => i.CompletionsToday)
        .ToList();
    }

    private static string GenerateReinforcementMessage(string identityName, int completions) =>
        completions switch
        {
            1 => $"You showed up as {identityName} today!",
            2 => $"Two votes for {identityName}!",
            >= 3 => $"Amazing! {completions} votes for {identityName} today!",
            _ => $"Keep building {identityName}!"
        };

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

// DTOs for Today View
public record TodayViewResponse(
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback,
    List<IdentityProgressResponse> IdentityProgress,
    DailyCommitmentResponse? DailyCommitment,
    YesterdayCommitmentResponse YesterdayCommitment
);

public record TodayTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid GoalId,
    string GoalTitle,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    string? IdentityColor,
    DateOnly? DueDate,
    string Status
);

public record TodayIdentityFeedbackResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int CompletionsToday,
    string ReinforcementMessage
);

public record IdentityProgressResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    string Status,
    string Trend,
    int AccountAgeDays,
    bool ShowNumericScore
);
