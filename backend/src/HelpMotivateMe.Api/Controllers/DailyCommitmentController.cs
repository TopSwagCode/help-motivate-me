using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.DailyCommitment;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/daily-commitment")]
public class DailyCommitmentController : ApiControllerBase
{
    private readonly IDailyCommitmentService _commitmentService;
    private readonly IAnalyticsService _analyticsService;

    public DailyCommitmentController(
        IDailyCommitmentService commitmentService,
        IAnalyticsService analyticsService)
    {
        _commitmentService = commitmentService;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get the daily commitment for a specific date (defaults to today).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DailyCommitmentResponse?>> GetCommitment([FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var commitment = await _commitmentService.GetCommitmentAsync(userId, targetDate);

        return Ok(commitment);
    }

    /// <summary>
    /// Get identity options with scores for creating a commitment.
    /// </summary>
    [HttpGet("options")]
    public async Task<ActionResult<CommitmentOptionsResponse>> GetOptions()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await _analyticsService.LogEventAsync(userId, sessionId, "DailyCommitmentFlowStarted", new { date = today });

        var options = await _commitmentService.GetIdentityOptionsAsync(userId, today);

        return Ok(options);
    }

    /// <summary>
    /// Get action suggestions (habits and tasks) for a specific identity.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ActionSuggestionsResponse>> GetSuggestions([FromQuery] Guid identityId)
    {
        var userId = GetUserId();

        var suggestions = await _commitmentService.GetActionSuggestionsAsync(userId, identityId);

        return Ok(suggestions);
    }

    /// <summary>
    /// Get yesterday's commitment info (for recovery message).
    /// </summary>
    [HttpGet("yesterday")]
    public async Task<ActionResult<YesterdayCommitmentResponse>> GetYesterdayCommitment()
    {
        var userId = GetUserId();

        var yesterday = await _commitmentService.GetYesterdayCommitmentAsync(userId);

        return Ok(yesterday);
    }

    /// <summary>
    /// Create a new daily commitment.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DailyCommitmentResponse>> CreateCommitment([FromBody] CreateDailyCommitmentRequest request)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        try
        {
            var commitment = await _commitmentService.CreateCommitmentAsync(userId, request);

            await _analyticsService.LogEventAsync(userId, sessionId, "DailyCommitmentCreated", new
            {
                identityId = request.IdentityId,
                isCustomAction = !request.LinkedHabitStackItemId.HasValue && !request.LinkedTaskId.HasValue,
                linkedHabitId = request.LinkedHabitStackItemId,
                linkedTaskId = request.LinkedTaskId
            });

            return CreatedAtAction(nameof(GetCommitment), null, commitment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark a commitment as completed.
    /// </summary>
    [HttpPut("{id:guid}/complete")]
    public async Task<ActionResult<DailyCommitmentResponse>> CompleteCommitment(Guid id)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        try
        {
            var commitment = await _commitmentService.CompleteCommitmentAsync(userId, id);

            await _analyticsService.LogEventAsync(userId, sessionId, "DailyCommitmentCompleted", new
            {
                identityId = commitment.IdentityId,
                commitmentId = id
            });

            return Ok(commitment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Dismiss a commitment for the day.
    /// </summary>
    [HttpPut("{id:guid}/dismiss")]
    public async Task<ActionResult<DailyCommitmentResponse>> DismissCommitment(Guid id)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        try
        {
            var commitment = await _commitmentService.DismissCommitmentAsync(userId, id);

            await _analyticsService.LogEventAsync(userId, sessionId, "DailyCommitmentDismissed", new
            {
                identityId = commitment.IdentityId,
                commitmentId = id
            });

            return Ok(commitment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
