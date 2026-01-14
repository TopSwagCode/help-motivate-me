using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Enums;
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
    private readonly AppDbContext _db;
    private readonly IdentityScoreService _identityScoreService;

    public TodayController(AppDbContext db, IdentityScoreService identityScoreService)
    {
        _db = db;
        _identityScoreService = identityScoreService;
    }

    /// <summary>
    /// Get the today view for a specific date (defaults to today).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TodayViewResponse>> GetTodayView([FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

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

        return Ok(new TodayViewResponse(
            targetDate,
            habitStacks,
            upcomingTasks,
            completedTasks,
            identityFeedback,
            identityProgress
        ));
    }

    private async Task<List<TodayHabitStackResponse>> GetTodayHabitStacks(Guid userId, DateOnly targetDate)
    {
        var stacks = await _db.HabitStacks
            .Include(s => s.Identity)
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Completions)
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
                i.Completions.Any(c => c.CompletedDate == targetDate),
                i.CurrentStreak
            )),
            s.Items.Count(i => i.Completions.Any(c => c.CompletedDate == targetDate)),
            s.Items.Count
        )).ToList();
    }

    private async Task<List<TodayTaskResponse>> GetUpcomingTasks(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        // Include tasks with due date within 7 days OR tasks without any due date
        // Exclude completed tasks and tasks from completed goals
        var tasks = await _db.TaskItems
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

        var tasks = await _db.TaskItems
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
        // Get identities with their habit stacks and tasks
        var identities = await _db.Identities
            .Include(i => i.HabitStacks)
                .ThenInclude(hs => hs.Items)
                    .ThenInclude(hsi => hsi.Completions)
            .Include(i => i.Tasks)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        return identities.Select(i =>
        {
            // Count habit stack item completions for this identity
            var habitCompletionsToday = i.HabitStacks
                .SelectMany(hs => hs.Items)
                .SelectMany(hsi => hsi.Completions.Where(c => c.CompletedDate == targetDate))
                .Count();

            // Count task completions for this identity (tasks completed on this date)
            var taskCompletionsToday = i.Tasks
                .Count(t => t.Status == TaskItemStatus.Completed &&
                           t.CompletedAt.HasValue &&
                           t.CompletedAt.Value == targetDate);

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
}

// DTOs for Today View
public record TodayViewResponse(
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback,
    List<IdentityProgressResponse> IdentityProgress
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
