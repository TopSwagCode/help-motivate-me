using HelpMotivateMe.Core.DTOs.Milestones;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/milestones")]
public class MilestonesController : ApiControllerBase
{
    private readonly IMilestoneService _milestoneService;

    public MilestonesController(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    /// <summary>
    ///     Get all milestones awarded to the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserMilestoneResponse>>> GetMilestones()
    {
        var userId = GetUserId();
        var milestones = await _milestoneService.GetUserMilestonesAsync(userId);
        return Ok(milestones);
    }

    /// <summary>
    ///     Get unseen milestones for the current user.
    /// </summary>
    [HttpGet("unseen")]
    public async Task<ActionResult<IEnumerable<UserMilestoneResponse>>> GetUnseenMilestones()
    {
        var userId = GetUserId();
        var milestones = await _milestoneService.GetUnseenMilestonesAsync(userId);
        return Ok(milestones);
    }

    /// <summary>
    ///     Mark milestones as seen.
    /// </summary>
    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen([FromBody] MarkSeenRequest request)
    {
        var userId = GetUserId();
        await _milestoneService.MarkMilestonesSeenAsync(userId, request.MilestoneIds);
        return NoContent();
    }

    /// <summary>
    ///     Get user stats.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsResponse>> GetStats()
    {
        var userId = GetUserId();
        var stats = await _milestoneService.GetUserStatsAsync(userId);
        return Ok(stats);
    }

    /// <summary>
    ///     Get all milestone definitions (admin only).
    /// </summary>
    [HttpGet("definitions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MilestoneDefinitionResponse>>> GetDefinitions()
    {
        var definitions = await _milestoneService.GetAllDefinitionsAsync();
        return Ok(definitions);
    }

    /// <summary>
    ///     Create a new milestone definition (admin only).
    /// </summary>
    [HttpPost("definitions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MilestoneDefinitionResponse>> CreateDefinition(
        [FromBody] CreateMilestoneRequest request)
    {
        var definition = await _milestoneService.CreateDefinitionAsync(request);
        return CreatedAtAction(nameof(GetDefinitions), new { id = definition.Id }, definition);
    }

    /// <summary>
    ///     Update a milestone definition (admin only).
    /// </summary>
    [HttpPut("definitions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MilestoneDefinitionResponse>> UpdateDefinition(Guid id,
        [FromBody] UpdateMilestoneRequest request)
    {
        var definition = await _milestoneService.UpdateDefinitionAsync(id, request);
        if (definition == null) return NotFound();

        return Ok(definition);
    }

    /// <summary>
    ///     Toggle milestone active status (admin only).
    /// </summary>
    [HttpPatch("definitions/{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleDefinition(Guid id, [FromBody] ToggleMilestoneRequest request)
    {
        var success = await _milestoneService.ToggleDefinitionAsync(id, request.IsActive);
        if (!success) return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Delete a milestone definition (admin only).
    /// </summary>
    [HttpDelete("definitions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDefinition(Guid id)
    {
        var success = await _milestoneService.DeleteDefinitionAsync(id);
        if (!success) return NotFound();

        return NoContent();
    }
}