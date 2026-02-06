using HelpMotivateMe.Core.DTOs.IdentityProofs;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/identity-proofs")]
public class IdentityProofsController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IMilestoneService _milestoneService;
    private readonly IIdentityProofService _proofService;

    public IdentityProofsController(
        IIdentityProofService proofService,
        IAnalyticsService analyticsService,
        IMilestoneService milestoneService)
    {
        _proofService = proofService;
        _analyticsService = analyticsService;
        _milestoneService = milestoneService;
    }

    /// <summary>
    ///     Get identity proofs for a date range.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IdentityProofResponse>>> GetProofs(
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var userId = GetUserId();
        var proofs = await _proofService.GetProofsAsync(userId, startDate, endDate);
        return Ok(proofs);
    }

    /// <summary>
    ///     Create a new identity proof.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IdentityProofResponse>> CreateProof([FromBody] CreateIdentityProofRequest request)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        try
        {
            var proof = await _proofService.CreateProofAsync(userId, request);

            await _analyticsService.LogEventAsync(userId, sessionId, "IdentityProofCreated", new
            {
                identityId = request.IdentityId,
                intensity = request.Intensity.ToString(),
                voteValue = (int)request.Intensity
            });

            await _milestoneService.RecordEventAsync(userId, "IdentityProofAdded",
                new { identityId = request.IdentityId });

            return CreatedAtAction(nameof(GetProofs), null, proof);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     Delete an identity proof.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProof(Guid id)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var deleted = await _proofService.DeleteProofAsync(userId, id);

        if (!deleted) return NotFound();

        await _analyticsService.LogEventAsync(userId, sessionId, "IdentityProofDeleted", new
        {
            proofId = id
        });

        return NoContent();
    }
}