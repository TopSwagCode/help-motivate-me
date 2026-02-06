using System.Security.Claims;
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
    private readonly AppDbContext _db;
    private readonly IQueryInterface<Identity> _identitiesQuery;
    private readonly IAnalyticsService _analyticsService;
    private static readonly string[] ReinforcementTemplates = [
        "That's what a {0} does!",
        "You're becoming a {0}!",
        "Another vote for being a {0}!",
        "Keep it up! You're a {0}!",
        "A true {0} moment!"
    ];

    public IdentitiesController(AppDbContext db, IQueryInterface<Identity> identitiesQuery, IAnalyticsService analyticsService)
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

        var identities = await _identitiesQuery
            .Where(i => i.UserId == userId)
            .Include(i => i.Tasks)
            .Include(i => i.Goals)
            .Include(i => i.Proofs)
            .Include(i => i.DailyCommitments)
            .OrderBy(i => i.Name)
            .ToListAsync();

        return Ok(identities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IdentityResponse>> GetIdentity(Guid id)
    {
        var userId = GetUserId();

        var identity = await _identitiesQuery
            .Include(i => i.Tasks)
            .Include(i => i.Goals)
            .Include(i => i.Proofs)
            .Include(i => i.DailyCommitments)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(identity));
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

        return CreatedAtAction(nameof(GetIdentity), new { id = identity.Id }, MapToResponse(identity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IdentityResponse>> UpdateIdentity(Guid id, [FromBody] UpdateIdentityRequest request)
    {
        var userId = GetUserId();

        var identity = await _db.Identities
            .Include(i => i.Tasks)
            .Include(i => i.Goals)
            .Include(i => i.Proofs)
            .Include(i => i.DailyCommitments)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null)
        {
            return NotFound();
        }

        identity.Name = request.Name;
        identity.Description = request.Description;
        identity.Color = request.Color;
        identity.Icon = request.Icon;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(identity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteIdentity(Guid id)
    {
        var userId = GetUserId();

        var identity = await _db.Identities
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null)
        {
            return NotFound();
        }

        _db.Identities.Remove(identity);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<IdentityStatsResponse>> GetIdentityStats(Guid id)
    {
        var userId = GetUserId();

        var identity = await _identitiesQuery
            .Include(i => i.Tasks)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (identity == null)
        {
            return NotFound();
        }

        var completedTasks = identity.Tasks.Count(t => t.Status == TaskItemStatus.Completed);
        var reinforcementMessage = GenerateReinforcementMessage(identity.Name, completedTasks);

        return Ok(new IdentityStatsResponse(
            identity.Id,
            identity.Name,
            completedTasks,
            0, // Streak no longer tracked for tasks
            completedTasks,
            reinforcementMessage
        ));
    }

    private static IdentityResponse MapToResponse(Identity identity)
    {
        var totalTasks = identity.Tasks.Count;
        var completedTasks = identity.Tasks.Count(t => t.Status == TaskItemStatus.Completed);

        // Calculate tasks completed in last 7 days
        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        var tasksCompletedLast7Days = identity.Tasks.Count(t =>
            t.Status == TaskItemStatus.Completed &&
            t.CompletedAt.HasValue &&
            t.CompletedAt.Value >= sevenDaysAgo);

        // Goals stats
        var totalGoals = identity.Goals.Count;
        var completedGoals = identity.Goals.Count(g => g.IsCompleted);

        // Identity proofs count
        var totalProofs = identity.Proofs.Count;

        // Daily commitments stats
        var totalDailyCommitments = identity.DailyCommitments.Count;
        var completedDailyCommitments = identity.DailyCommitments.Count(dc => dc.Status == DailyCommitmentStatus.Completed);

        return new IdentityResponse(
            identity.Id,
            identity.Name,
            identity.Description,
            identity.Color,
            identity.Icon,
            totalTasks,
            completedTasks,
            tasksCompletedLast7Days,
            totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0,
            totalGoals,
            completedGoals,
            totalProofs,
            totalDailyCommitments,
            completedDailyCommitments,
            identity.CreatedAt
        );
    }

    private static string GenerateReinforcementMessage(string identityName, int completedTasks)
    {
        var templateIndex = Random.Shared.Next(ReinforcementTemplates.Length);
        var template = ReinforcementTemplates[templateIndex];

        return string.Format(template, identityName, completedTasks);
    }
}
