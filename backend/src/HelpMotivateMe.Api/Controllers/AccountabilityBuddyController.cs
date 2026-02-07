using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.DTOs.Today;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/buddies")]
public class AccountabilityBuddyController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IResourceAuthorizationService _auth;
    private readonly IAccountabilityBuddyService _buddyService;

    public AccountabilityBuddyController(
        IAccountabilityBuddyService buddyService,
        IAnalyticsService analyticsService,
        IResourceAuthorizationService auth)
    {
        _buddyService = buddyService;
        _analyticsService = analyticsService;
        _auth = auth;
    }

    /// <summary>
    ///     Get all buddy relationships - both my buddies and people I'm buddy for.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BuddyRelationshipsResponse>> GetBuddyRelationships()
    {
        var userId = _auth.GetCurrentUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "BuddiesPageLoaded");

        var myBuddies = await _buddyService.GetMyBuddiesAsync(userId);
        var buddyingFor = await _buddyService.GetBuddyingForAsync(userId);

        return Ok(new BuddyRelationshipsResponse(
            myBuddies.Select(b => new BuddyResponse(b.Id, b.BuddyUserId, b.Email, b.DisplayName, b.CreatedAt)).ToList(),
            buddyingFor.Select(b => new BuddyForResponse(b.Id, b.UserId, b.Email, b.DisplayName, b.CreatedAt)).ToList()
        ));
    }

    /// <summary>
    ///     Invite a new accountability buddy by email.
    /// </summary>
    [HttpPost("invite")]
    public async Task<ActionResult<BuddyResponse>> InviteBuddy([FromBody] InviteBuddyRequest request)
    {
        var userId = _auth.GetCurrentUserId();

        var result = await _buddyService.InviteBuddyAsync(userId, request.Email);

        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });

        var buddy = result.Buddy!;
        return Ok(new BuddyResponse(buddy.Id, buddy.BuddyUserId, buddy.Email, buddy.DisplayName, buddy.CreatedAt));
    }

    /// <summary>
    ///     Remove one of my accountability buddies.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveBuddy(Guid id)
    {
        var userId = _auth.GetCurrentUserId();

        var success = await _buddyService.RemoveBuddyAsync(userId, id);
        if (!success) return NotFound(new { message = "Buddy relationship not found" });

        return NoContent();
    }

    /// <summary>
    ///     Leave as someone's accountability buddy (remove myself).
    /// </summary>
    [HttpDelete("leave/{ownerUserId:guid}")]
    public async Task<IActionResult> LeaveBuddy(Guid ownerUserId)
    {
        var userId = _auth.GetCurrentUserId();

        var success = await _buddyService.LeaveBuddyAsync(userId, ownerUserId);
        if (!success) return NotFound(new { message = "Buddy relationship not found" });

        return NoContent();
    }

    /// <summary>
    ///     Get another user's Today view (as their accountability buddy).
    /// </summary>
    [HttpGet("{targetUserId:guid}/today")]
    public async Task<ActionResult<BuddyTodayViewResponse>> GetBuddyTodayView(Guid targetUserId,
        [FromQuery] DateOnly? date = null)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var userId = _auth.GetCurrentUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(userId, sessionId, "BuddyDetailLoaded",
            new { buddyUserId = targetUserId });

        var data = await _buddyService.GetBuddyTodayViewAsync(targetUserId, targetDate);

        return Ok(new BuddyTodayViewResponse(
            data.UserId,
            data.UserDisplayName,
            data.Date,
            data.HabitStacks,
            data.UpcomingTasks,
            data.CompletedTasks,
            data.IdentityFeedback
        ));
    }

    /// <summary>
    ///     Get another user's journal entries (as their accountability buddy).
    /// </summary>
    [HttpGet("{targetUserId:guid}/journal")]
    public async Task<ActionResult<List<BuddyJournalEntryResponse>>> GetBuddyJournal(Guid targetUserId)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var entries = await _buddyService.GetBuddyJournalAsync(targetUserId);

        var response = entries.Select(j => new BuddyJournalEntryResponse(
            j.Id,
            j.Title,
            j.Description,
            j.EntryDate,
            j.AuthorUserId,
            j.AuthorDisplayName,
            j.Images.Select(i => new BuddyJournalImageResponse(i.Id, i.FileName, i.Url, i.SortOrder)).ToList(),
            j.Reactions.Select(r =>
                new BuddyJournalReactionResponse(r.Id, r.Emoji, r.UserId, r.UserDisplayName, r.CreatedAt)).ToList(),
            j.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    ///     Write a journal entry for another user (as their accountability buddy).
    /// </summary>
    [HttpPost("{targetUserId:guid}/journal")]
    public async Task<ActionResult<BuddyJournalEntryResponse>> CreateBuddyJournalEntry(
        Guid targetUserId,
        [FromBody] CreateBuddyJournalEntryRequest request)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var userId = _auth.GetCurrentUserId();
        var entry = await _buddyService.CreateBuddyJournalEntryAsync(
            userId, targetUserId, request.Title, request.Description, request.EntryDate);

        return Ok(new BuddyJournalEntryResponse(
            entry.Id,
            entry.Title,
            entry.Description,
            entry.EntryDate,
            entry.AuthorUserId,
            entry.AuthorDisplayName,
            entry.Images.Select(i => new BuddyJournalImageResponse(i.Id, i.FileName, i.Url, i.SortOrder)).ToList(),
            entry.Reactions.Select(r =>
                new BuddyJournalReactionResponse(r.Id, r.Emoji, r.UserId, r.UserDisplayName, r.CreatedAt)).ToList(),
            entry.CreatedAt
        ));
    }

    /// <summary>
    ///     Upload an image to a buddy's journal entry (only if you authored the entry).
    /// </summary>
    [HttpPost("{targetUserId:guid}/journal/{entryId:guid}/images")]
    public async Task<ActionResult<BuddyJournalImageResponse>> UploadBuddyJournalImage(
        Guid targetUserId,
        Guid entryId,
        IFormFile file)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var userId = _auth.GetCurrentUserId();

        // Validate file
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided or file is empty" });

        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize) return BadRequest(new { message = "File too large. Maximum size: 5MB" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP" });

        using var stream = file.OpenReadStream();
        var result = await _buddyService.UploadBuddyJournalImageAsync(
            userId, targetUserId, entryId, stream, file.FileName, file.ContentType, file.Length);

        if (!result.Success)
        {
            if (result.ErrorMessage == "Forbidden")
                return Forbid();
            if (result.ErrorMessage == "Journal entry not found")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        var image = result.Image!;
        return Ok(new BuddyJournalImageResponse(image.Id, image.FileName, image.Url, image.SortOrder));
    }

    /// <summary>
    ///     Add a reaction to a buddy's journal entry.
    /// </summary>
    [HttpPost("{targetUserId:guid}/journal/{entryId:guid}/reactions")]
    public async Task<ActionResult<BuddyJournalReactionResponse>> AddBuddyJournalReaction(
        Guid targetUserId,
        Guid entryId,
        [FromBody] AddBuddyJournalReactionRequest request)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var userId = _auth.GetCurrentUserId();
        var result = await _buddyService.AddBuddyJournalReactionAsync(userId, targetUserId, entryId, request.Emoji);

        if (!result.Success)
        {
            if (result.ErrorMessage == "Journal entry not found")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        var reaction = result.Reaction!;
        return Ok(new BuddyJournalReactionResponse(reaction.Id, reaction.Emoji, reaction.UserId,
            reaction.UserDisplayName, reaction.CreatedAt));
    }

    /// <summary>
    ///     Remove a reaction from a buddy's journal entry (only own reactions).
    /// </summary>
    [HttpDelete("{targetUserId:guid}/journal/{entryId:guid}/reactions/{reactionId:guid}")]
    public async Task<IActionResult> RemoveBuddyJournalReaction(
        Guid targetUserId,
        Guid entryId,
        Guid reactionId)
    {
        if (!await _auth.IsOwnerOrBuddyAsync(targetUserId)) return Forbid();

        var userId = _auth.GetCurrentUserId();
        var success = await _buddyService.RemoveBuddyJournalReactionAsync(userId, entryId, reactionId);
        if (!success) return NotFound();

        return NoContent();
    }
}

// DTOs for Accountability Buddy

public record BuddyRelationshipsResponse(
    List<BuddyResponse> MyBuddies,
    List<BuddyForResponse> BuddyingFor
);

public record BuddyResponse(
    Guid Id,
    Guid BuddyUserId,
    string BuddyEmail,
    string BuddyDisplayName,
    DateTime CreatedAt
);

public record BuddyForResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    DateTime CreatedAt
);

public record InviteBuddyRequest(string Email);

public record BuddyTodayViewResponse(
    Guid UserId,
    string UserDisplayName,
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback
);

public record BuddyJournalEntryResponse(
    Guid Id,
    string Title,
    string? Description,
    string EntryDate,
    Guid? AuthorUserId,
    string? AuthorDisplayName,
    List<BuddyJournalImageResponse> Images,
    List<BuddyJournalReactionResponse> Reactions,
    DateTime CreatedAt
);

public record BuddyJournalImageResponse(
    Guid Id,
    string FileName,
    string Url,
    int SortOrder
);

public record BuddyJournalReactionResponse(
    Guid Id,
    string Emoji,
    Guid UserId,
    string UserDisplayName,
    DateTime CreatedAt
);

public record AddBuddyJournalReactionRequest(
    string Emoji
);

public record CreateBuddyJournalEntryRequest(
    string Title,
    string? Description,
    string? EntryDate
);