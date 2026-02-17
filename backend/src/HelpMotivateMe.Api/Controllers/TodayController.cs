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
    private readonly IAnalyticsService _analyticsService;
    private readonly IResourceAuthorizationService _auth;
    private readonly IDailyCommitmentService _commitmentService;
    private readonly IIdentityScoreService _identityScoreService;
    private readonly ITodayViewService _todayViewService;

    public TodayController(
        ITodayViewService todayViewService,
        IIdentityScoreService identityScoreService,
        IDailyCommitmentService commitmentService,
        IAnalyticsService analyticsService,
        IResourceAuthorizationService auth)
    {
        _todayViewService = todayViewService;
        _identityScoreService = identityScoreService;
        _commitmentService = commitmentService;
        _analyticsService = analyticsService;
        _auth = auth;
    }

    /// <summary>
    ///     Get the today view for a specific date (defaults to today).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TodayViewResponse>> GetTodayView([FromQuery] DateOnly? date = null)
    {
        var userId = _auth.GetCurrentUserId();
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

    /// <summary>
    ///     Get the daily digest comparing yesterday's and today's identity scores.
    /// </summary>
    [HttpGet("digest")]
    public async Task<ActionResult<DailyDigestResponse>> GetDailyDigest([FromQuery] DateOnly? date = null)
    {
        var userId = _auth.GetCurrentUserId();
        var today = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var todayScores = await _identityScoreService.CalculateScoresAsync(userId, today);
        var yesterdayScores = await _identityScoreService.CalculateScoresAsync(userId, yesterday);
        var yesterdayFeedback = await _todayViewService.GetIdentityFeedbackAsync(userId, yesterday);

        var yesterdayScoreMap = yesterdayScores.ToDictionary(s => s.Id);
        var feedbackMap = yesterdayFeedback.ToDictionary(f => f.Id);

        var identities = todayScores.Select(s =>
        {
            yesterdayScoreMap.TryGetValue(s.Id, out var ys);
            feedbackMap.TryGetValue(s.Id, out var fb);

            return new DailyDigestIdentityResponse(
                s.Id,
                s.Name,
                s.Color,
                s.Icon,
                ys?.Score ?? 0,
                s.Score,
                ys?.Status.ToString() ?? "Dormant",
                s.Status.ToString(),
                s.Trend.ToString(),
                fb?.TotalVotes ?? 0,
                fb?.HabitVotes ?? 0,
                fb?.StackBonusVotes ?? 0,
                fb?.TaskVotes ?? 0,
                fb?.ProofVotes ?? 0
            );
        }).ToList();

        var totalYesterdayVotes = identities.Sum(i => i.YesterdayVotes);

        return Ok(new DailyDigestResponse(today, identities, totalYesterdayVotes));
    }
}