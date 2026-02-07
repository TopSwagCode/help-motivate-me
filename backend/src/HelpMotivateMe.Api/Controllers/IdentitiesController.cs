using HelpMotivateMe.Core.DTOs.Identities;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/identities")]
public class IdentitiesController : ApiControllerBase
{
    private static readonly string[] ReinforcementTemplates =
    [
        "That's what a {0} does!",
        "You're becoming a {0}!",
        "Another vote for being a {0}!",
        "Keep it up! You're a {0}!",
        "A true {0} moment!"
    ];

    private readonly IAnalyticsService _analyticsService;
    private readonly AppDbContext _db;
    private readonly IQueryInterface<Identity> _identitiesQuery;

    public IdentitiesController(AppDbContext db, IQueryInterface<Identity> identitiesQuery,
        IAnalyticsService analyticsService)
    {
        _db = db;
        _identitiesQuery = identitiesQuery;
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<IdentityResponse>>> GetIdentities()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "IdentitiesPageLoaded");

        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);

        var identities = await _identitiesQuery
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.Name)
            .Select(i => new IdentityResponse(
                i.Id,
                i.Name,
                i.Description,
                i.Color,
                i.Icon,
                i.Tasks.Count,
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed),
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed && t.CompletedAt.HasValue && t.CompletedAt.Value >= sevenDaysAgo),
                i.Tasks.Count > 0
                    ? Math.Round((double)i.Tasks.Count(t => t.Status == TaskItemStatus.Completed) / i.Tasks.Count * 100, 1)
                    : 0,
                i.Goals.Count,
                i.Goals.Count(g => g.IsCompleted),
                i.Proofs.Count,
                i.DailyCommitments.Count,
                i.DailyCommitments.Count(dc => dc.Status == DailyCommitmentStatus.Completed),
                i.CreatedAt
            ))
            .ToListAsync();

        return Ok(identities);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IdentityResponse>> GetIdentity(Guid id)
    {
        var userId = GetUserId();

        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);

        var identity = await _identitiesQuery
            .Where(i => i.Id == id && i.UserId == userId)
            .Select(i => new IdentityResponse(
                i.Id,
                i.Name,
                i.Description,
                i.Color,
                i.Icon,
                i.Tasks.Count,
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed),
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed && t.CompletedAt.HasValue && t.CompletedAt.Value >= sevenDaysAgo),
                i.Tasks.Count > 0
                    ? Math.Round((double)i.Tasks.Count(t => t.Status == TaskItemStatus.Completed) / i.Tasks.Count * 100, 1)
                    : 0,
                i.Goals.Count,
                i.Goals.Count(g => g.IsCompleted),
                i.Proofs.Count,
                i.DailyCommitments.Count,
                i.DailyCommitments.Count(dc => dc.Status == DailyCommitmentStatus.Completed),
                i.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (identity == null) return NotFound();

        return Ok(identity);
    }

    [HttpPost]
    public async Task<ActionResult<IdentityResponse>> CreateIdentity([FromBody] CreateIdentityRequest request)
    {
        var userId = GetUserId();

        var identity = new Identity
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Icon = request.Icon
        };

        _db.Identities.Add(identity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetIdentity), new { id = identity.Id }, new IdentityResponse(
            identity.Id,
            identity.Name,
            identity.Description,
            identity.Color,
            identity.Icon,
            TotalTasks: 0,
            CompletedTasks: 0,
            TasksCompletedLast7Days: 0,
            CompletionRate: 0,
            TotalGoals: 0,
            CompletedGoals: 0,
            TotalProofs: 0,
            TotalDailyCommitments: 0,
            CompletedDailyCommitments: 0,
            identity.CreatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IdentityResponse>> UpdateIdentity(Guid id, [FromBody] UpdateIdentityRequest request)
    {
        var userId = GetUserId();

        var identity = await _db.Identities
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null) return NotFound();

        identity.Name = request.Name;
        identity.Description = request.Description;
        identity.Color = request.Color;
        identity.Icon = request.Icon;

        await _db.SaveChangesAsync();

        // Use projection for the response to avoid loading all related entities
        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);

        var response = await _identitiesQuery
            .Where(i => i.Id == id)
            .Select(i => new IdentityResponse(
                i.Id,
                i.Name,
                i.Description,
                i.Color,
                i.Icon,
                i.Tasks.Count,
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed),
                i.Tasks.Count(t => t.Status == TaskItemStatus.Completed && t.CompletedAt.HasValue && t.CompletedAt.Value >= sevenDaysAgo),
                i.Tasks.Count > 0
                    ? Math.Round((double)i.Tasks.Count(t => t.Status == TaskItemStatus.Completed) / i.Tasks.Count * 100, 1)
                    : 0,
                i.Goals.Count,
                i.Goals.Count(g => g.IsCompleted),
                i.Proofs.Count,
                i.DailyCommitments.Count,
                i.DailyCommitments.Count(dc => dc.Status == DailyCommitmentStatus.Completed),
                i.CreatedAt
            ))
            .FirstAsync();

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteIdentity(Guid id)
    {
        var userId = GetUserId();

        var identity = await _db.Identities
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null) return NotFound();

        _db.Identities.Remove(identity);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<IdentityStatsResponse>> GetIdentityStats(Guid id)
    {
        var userId = GetUserId();

        var stats = await _identitiesQuery
            .Where(i => i.Id == id && i.UserId == userId)
            .Select(i => new
            {
                i.Id,
                i.Name,
                CompletedTasks = i.Tasks.Count(t => t.Status == TaskItemStatus.Completed)
            })
            .FirstOrDefaultAsync();

        if (stats == null) return NotFound();

        var reinforcementMessage = GenerateReinforcementMessage(stats.Name, stats.CompletedTasks);

        return Ok(new IdentityStatsResponse(
            stats.Id,
            stats.Name,
            stats.CompletedTasks,
            0, // Streak no longer tracked for tasks
            stats.CompletedTasks,
            reinforcementMessage
        ));
    }

    private static string GenerateReinforcementMessage(string identityName, int completedTasks)
    {
        var templateIndex = Random.Shared.Next(ReinforcementTemplates.Length);
        var template = ReinforcementTemplates[templateIndex];

        return string.Format(template, identityName, completedTasks);
    }
}