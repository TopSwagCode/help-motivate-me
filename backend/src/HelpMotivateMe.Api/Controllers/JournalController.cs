using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Journal;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/journal")]
[Authorize]
public class JournalController : ControllerBase
{
    private const string SessionIdKey = "AnalyticsSessionId";
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly IAnalyticsService _analyticsService;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    private const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int MaxImagesPerEntry = 5;

    public JournalController(AppDbContext db, IStorageService storage, IAnalyticsService analyticsService)
    {
        _db = db;
        _storage = storage;
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<JournalEntryResponse>>> GetEntries([FromQuery] string? filter = null)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "JournalPageLoaded");

        // Start with all entries belonging to this user's journal
        var query = _db.JournalEntries
            .Include(j => j.HabitStack)
            .Include(j => j.TaskItem)
            .Include(j => j.Author)
            .Include(j => j.Images.OrderBy(i => i.SortOrder))
            .Include(j => j.Reactions)
                .ThenInclude(r => r.User)
            .Where(j => j.UserId == userId);

        // Apply filter based on who authored the entry
        if (filter == "own")
        {
            // Only entries written by the user themselves (AuthorUserId is null for legacy or equals userId)
            query = query.Where(j => j.AuthorUserId == null || j.AuthorUserId == userId);
        }
        else if (filter == "buddies")
        {
            // Only entries written by buddies (AuthorUserId is set and not equal to userId)
            query = query.Where(j => j.AuthorUserId != null && j.AuthorUserId != userId);
        }
        // "all" or no filter = return everything

        var entries = await query
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalEntryResponse>> GetEntry(Guid id)
    {
        var userId = GetUserId();

        var entry = await _db.JournalEntries
            .Include(j => j.HabitStack)
            .Include(j => j.TaskItem)
            .Include(j => j.Author)
            .Include(j => j.Images.OrderBy(i => i.SortOrder))
            .Include(j => j.Reactions)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);

        if (entry == null) return NotFound();

        return Ok(MapToResponse(entry));
    }

    [HttpPost]
    public async Task<ActionResult<JournalEntryResponse>> CreateEntry([FromBody] CreateJournalEntryRequest request)
    {
        var userId = GetUserId();

        // Validate linking constraint
        if (request.HabitStackId.HasValue && request.TaskItemId.HasValue)
        {
            return BadRequest(new { message = "Cannot link to both a habit stack and a task" });
        }

        // Validate linked entities belong to user
        if (request.HabitStackId.HasValue)
        {
            var exists = await _db.HabitStacks.AnyAsync(h => h.Id == request.HabitStackId && h.UserId == userId);
            if (!exists) return BadRequest(new { message = "Invalid habit stack" });
        }

        if (request.TaskItemId.HasValue)
        {
            var exists = await _db.TaskItems
                .Include(t => t.Goal)
                .AnyAsync(t => t.Id == request.TaskItemId && t.Goal.UserId == userId);
            if (!exists) return BadRequest(new { message = "Invalid task" });
        }

        var entry = new JournalEntry
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            EntryDate = request.EntryDate,
            HabitStackId = request.HabitStackId,
            TaskItemId = request.TaskItemId,
            AuthorUserId = userId  // Owner is the author of their own entries
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(userId, sessionId, "JournalEntryCreated", new { entryId = entry.Id });

        // Reload with navigation properties
        await _db.Entry(entry).Reference(e => e.HabitStack).LoadAsync();
        if (entry.TaskItemId.HasValue)
        {
            await _db.Entry(entry).Reference(e => e.TaskItem).LoadAsync();
        }

        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, MapToResponse(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JournalEntryResponse>> UpdateEntry(Guid id, [FromBody] UpdateJournalEntryRequest request)
    {
        var userId = GetUserId();

        var entry = await _db.JournalEntries
            .Include(j => j.Images)
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);

        if (entry == null) return NotFound();

        // Validate linking constraint
        if (request.HabitStackId.HasValue && request.TaskItemId.HasValue)
        {
            return BadRequest(new { message = "Cannot link to both a habit stack and a task" });
        }

        // Validate linked entities
        if (request.HabitStackId.HasValue)
        {
            var exists = await _db.HabitStacks.AnyAsync(h => h.Id == request.HabitStackId && h.UserId == userId);
            if (!exists) return BadRequest(new { message = "Invalid habit stack" });
        }

        if (request.TaskItemId.HasValue)
        {
            var exists = await _db.TaskItems
                .Include(t => t.Goal)
                .AnyAsync(t => t.Id == request.TaskItemId && t.Goal.UserId == userId);
            if (!exists) return BadRequest(new { message = "Invalid task" });
        }

        entry.Title = request.Title;
        entry.Description = request.Description;
        entry.EntryDate = request.EntryDate;
        entry.HabitStackId = request.HabitStackId;
        entry.TaskItemId = request.TaskItemId;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Reload navigation properties
        await _db.Entry(entry).Reference(e => e.HabitStack).LoadAsync();
        if (entry.TaskItemId.HasValue)
        {
            await _db.Entry(entry).Reference(e => e.TaskItem).LoadAsync();
        }

        return Ok(MapToResponse(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var userId = GetUserId();

        var entry = await _db.JournalEntries
            .Include(j => j.Images)
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);

        if (entry == null) return NotFound();

        // Delete images from S3
        if (entry.Images.Count > 0)
        {
            await _storage.DeleteManyAsync(entry.Images.Select(i => i.S3Key));
        }

        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<ActionResult<JournalImageResponse>> UploadImage(Guid id, IFormFile file)
    {
        var userId = GetUserId();

        var entry = await _db.JournalEntries
            .Include(j => j.Images)
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);

        if (entry == null) return NotFound();

        if (entry.Images.Count >= MaxImagesPerEntry)
        {
            return BadRequest(new { message = $"Maximum {MaxImagesPerEntry} images allowed per entry" });
        }

        // Validate file exists and has content
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided or file is empty" });
        }

        // Validate file size first (strict check)
        if (file.Length > MaxImageSizeBytes)
        {
            return BadRequest(new { message = $"File too large ({file.Length / 1024 / 1024:F2}MB). Maximum size: {MaxImageSizeBytes / 1024 / 1024}MB. Please compress the image before uploading." });
        }

        // Validate content type
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP" });
        }

        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var s3Key = $"journals/{userId}/{id}/{uniqueFileName}";

        await using var stream = file.OpenReadStream();
        await _storage.UploadAsync(stream, s3Key, file.ContentType);

        var maxSortOrder = entry.Images.Count > 0 ? entry.Images.Max(i => i.SortOrder) : -1;

        var image = new JournalImage
        {
            JournalEntryId = id,
            FileName = file.FileName,
            S3Key = s3Key,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            SortOrder = maxSortOrder + 1
        };

        _db.JournalImages.Add(image);
        await _db.SaveChangesAsync();

        return Ok(new JournalImageResponse(
            image.Id,
            image.FileName,
            _storage.GetPresignedUrl(image.S3Key),
            image.SortOrder
        ));
    }

    [HttpDelete("{entryId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid entryId, Guid imageId)
    {
        var userId = GetUserId();

        var image = await _db.JournalImages
            .Include(i => i.JournalEntry)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.JournalEntryId == entryId && i.JournalEntry.UserId == userId);

        if (image == null) return NotFound();

        await _storage.DeleteAsync(image.S3Key);
        _db.JournalImages.Remove(image);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Reaction endpoints
    [HttpPost("{entryId:guid}/reactions")]
    public async Task<ActionResult<JournalReactionResponse>> AddReaction(Guid entryId, [FromBody] AddJournalReactionRequest request)
    {
        var userId = GetUserId();

        // Verify the entry exists and user has access (either owns it or is a buddy)
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == entryId && j.UserId == userId);

        // If not own entry, check if user is a buddy of the entry owner
        if (entry == null)
        {
            entry = await _db.JournalEntries
                .Where(j => j.Id == entryId)
                .Where(j => _db.AccountabilityBuddies.Any(b => 
                    b.UserId == j.UserId && b.BuddyUserId == userId))
                .FirstOrDefaultAsync();
        }

        if (entry == null) return NotFound();

        // Check if user already reacted with this emoji
        var existingReaction = await _db.JournalReactions
            .FirstOrDefaultAsync(r => r.JournalEntryId == entryId && r.UserId == userId && r.Emoji == request.Emoji);

        if (existingReaction != null)
        {
            return BadRequest(new { message = "You have already added this reaction" });
        }

        var user = await _db.Users.FindAsync(userId);

        var reaction = new JournalReaction
        {
            JournalEntryId = entryId,
            UserId = userId,
            Emoji = request.Emoji
        };

        _db.JournalReactions.Add(reaction);
        await _db.SaveChangesAsync();

        return Ok(new JournalReactionResponse(
            reaction.Id,
            reaction.Emoji,
            reaction.UserId,
            user!.DisplayName ?? user.Username,
            reaction.CreatedAt
        ));
    }

    [HttpDelete("{entryId:guid}/reactions/{reactionId:guid}")]
    public async Task<IActionResult> RemoveReaction(Guid entryId, Guid reactionId)
    {
        var userId = GetUserId();

        // Only allow removing own reactions
        var reaction = await _db.JournalReactions
            .FirstOrDefaultAsync(r => r.Id == reactionId && r.JournalEntryId == entryId && r.UserId == userId);

        if (reaction == null) return NotFound();

        _db.JournalReactions.Remove(reaction);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Endpoints to get linkable items for dropdowns
    [HttpGet("linkable/habit-stacks")]
    public async Task<ActionResult<List<LinkableHabitStackResponse>>> GetLinkableHabitStacks()
    {
        var userId = GetUserId();

        var stacks = await _db.HabitStacks
            .Where(h => h.UserId == userId && h.IsActive)
            .OrderBy(h => h.Name)
            .Select(h => new LinkableHabitStackResponse(h.Id, h.Name))
            .ToListAsync();

        return Ok(stacks);
    }

    [HttpGet("linkable/tasks")]
    public async Task<ActionResult<List<LinkableTaskResponse>>> GetLinkableTasks()
    {
        var userId = GetUserId();

        var tasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Where(t => t.Goal.UserId == userId && t.Status != TaskItemStatus.Completed)
            .OrderBy(t => t.Goal.Title)
            .ThenBy(t => t.Title)
            .Select(t => new LinkableTaskResponse(t.Id, t.Title, t.Goal.Title))
            .ToListAsync();

        return Ok(tasks);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private Guid GetSessionId()
    {
        var sessionIdString = HttpContext.Session.GetString(SessionIdKey);
        if (sessionIdString != null && Guid.TryParse(sessionIdString, out var sessionId))
        {
            return sessionId;
        }

        var newSessionId = Guid.NewGuid();
        HttpContext.Session.SetString(SessionIdKey, newSessionId.ToString());
        return newSessionId;
    }

    private JournalEntryResponse MapToResponse(JournalEntry entry)
    {
        return new JournalEntryResponse(
            entry.Id,
            entry.Title,
            entry.Description,
            entry.EntryDate,
            entry.HabitStackId,
            entry.HabitStack?.Name,
            entry.TaskItemId,
            entry.TaskItem?.Title,
            entry.AuthorUserId,
            entry.Author != null ? entry.Author.DisplayName ?? entry.Author.Username : null,
            entry.Images.Select(i => new JournalImageResponse(
                i.Id,
                i.FileName,
                _storage.GetPresignedUrl(i.S3Key),
                i.SortOrder
            )),
            entry.Reactions.Select(r => new JournalReactionResponse(
                r.Id,
                r.Emoji,
                r.UserId,
                r.User.DisplayName ?? r.User.Username,
                r.CreatedAt
            )),
            entry.CreatedAt,
            entry.UpdatedAt
        );
    }
}
