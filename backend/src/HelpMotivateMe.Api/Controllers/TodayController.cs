using HelpMotivateMe.Core.DTOs.DailyCommitment;
using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.DTOs.Today;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/today")]
public class TodayController : ApiControllerBase
{
    private readonly ITodayViewService _todayViewService;
    private readonly IIdentityScoreService _identityScoreService;
    private readonly IDailyCommitmentService _commitmentService;
    private readonly IAnalyticsService _analyticsService;

    public TodayController(
        ITodayViewService todayViewService,
        IIdentityScoreService identityScoreService,
        IDailyCommitmentService commitmentService,
        IAnalyticsService analyticsService)
    {
        _todayViewService = todayViewService;
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
        var habitStacks = await _todayViewService.GetTodayHabitStacksAsync(userId, targetDate);

        // Get upcoming tasks (pending)
        var upcomingTasks = await _todayViewService.GetUpcomingTasksAsync(userId, targetDate);

        // Get completed tasks for this date
        var completedTasks = await _todayViewService.GetCompletedTasksAsync(userId, targetDate);

        // Get identity feedback
        var identityFeedback = await _todayViewService.GetIdentityFeedbackAsync(userId, targetDate);

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
            s.ShowNumericScore,
            s.TodayVotes
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

public record IdentityProgressResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    string Status,
    string Trend,
    int AccountAgeDays,
    bool ShowNumericScore,
    int TodayVotes
);
