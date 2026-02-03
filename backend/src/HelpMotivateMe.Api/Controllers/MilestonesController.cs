using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Milestones;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/milestones")]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneService _milestoneService;

    public MilestonesController(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    /// <summary>
    /// Get all milestones awarded to the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserMilestoneResponse>>> GetMilestones()
    {
        var userId = GetUserId();
        var milestones = await _milestoneService.GetUserMilestonesAsync(userId);
        return Ok(milestones);
    }

    /// <summary>
    /// Get unseen milestones for the current user.
    /// </summary>
    [HttpGet("unseen")]
    public async Task<ActionResult<IEnumerable<UserMilestoneResponse>>> GetUnseenMilestones()
    {
        var userId = GetUserId();
        var milestones = await _milestoneService.GetUnseenMilestonesAsync(userId);
        return Ok(milestones);
    }

    /// <summary>
    /// Mark milestones as seen.
    /// </summary>
    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen([FromBody] MarkSeenRequest request)
    {
        var userId = GetUserId();
        await _milestoneService.MarkMilestonesSeenAsync(userId, request.MilestoneIds);
        return NoContent();
    }

    /// <summary>
    /// Get user stats.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsResponse>> GetStats()
    {
        var userId = GetUserId();
        var stats = await _milestoneService.GetUserStatsAsync(userId);
        return Ok(stats);
    }

    /// <summary>
    /// Get all milestone definitions (admin only).
    /// </summary>
    [HttpGet("definitions")]
    public async Task<ActionResult<IEnumerable<MilestoneDefinitionResponse>>> GetDefinitions()
    {
        var userId = GetUserId();
        var role = GetUserRole();

        if (role != UserRole.Admin)
        {
            return Forbid();
        }

        var definitions = await _milestoneService.GetAllDefinitionsAsync();
        return Ok(definitions);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private UserRole GetUserRole()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (roleClaim != null && Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return role;
        }
        return UserRole.User;
    }
}
