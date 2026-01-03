using System.Security.Claims;
using System.Security.Cryptography;
using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/buddies")]
public class AccountabilityBuddyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IStorageService _storage;

    public AccountabilityBuddyController(AppDbContext db, IConfiguration configuration, IEmailService emailService, IStorageService storage)
    {
        _db = db;
        _configuration = configuration;
        _emailService = emailService;
        _storage = storage;
    }

    /// <summary>
    /// Get all buddy relationships - both my buddies and people I'm buddy for.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BuddyRelationshipsResponse>> GetBuddyRelationships()
    {
        var userId = GetUserId();

        // Get my accountability buddies (people I've added)
        var myBuddies = await _db.AccountabilityBuddies
            .Include(ab => ab.BuddyUser)
            .Where(ab => ab.UserId == userId)
            .Select(ab => new BuddyResponse(
                ab.Id,
                ab.BuddyUserId,
                ab.BuddyUser.Email,
                ab.BuddyUser.DisplayName ?? ab.BuddyUser.Username,
                ab.CreatedAt
            ))
            .ToListAsync();

        // Get people I'm an accountability buddy for
        var buddyingFor = await _db.AccountabilityBuddies
            .Include(ab => ab.User)
            .Where(ab => ab.BuddyUserId == userId)
            .Select(ab => new BuddyForResponse(
                ab.Id,
                ab.UserId,
                ab.User.Email,
                ab.User.DisplayName ?? ab.User.Username,
                ab.CreatedAt
            ))
            .ToListAsync();

        return Ok(new BuddyRelationshipsResponse(myBuddies, buddyingFor));
    }

    /// <summary>
    /// Invite a new accountability buddy by email.
    /// </summary>
    [HttpPost("invite")]
    public async Task<ActionResult<BuddyResponse>> InviteBuddy([FromBody] InviteBuddyRequest request)
    {
        var userId = GetUserId();
        var email = request.Email.ToLowerInvariant();

        // Get inviter info
        var inviter = await _db.Users.FirstAsync(u => u.Id == userId);

        // Check if user is trying to add themselves
        if (inviter.Email.ToLowerInvariant() == email)
        {
            return BadRequest(new { message = "You cannot add yourself as an accountability buddy" });
        }

        // Find or create the buddy user
        var buddyUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (buddyUser == null)
        {
            // Create placeholder user
            buddyUser = new User
            {
                Username = await GenerateUniqueUsername(email.Split('@')[0]),
                Email = email
            };
            _db.Users.Add(buddyUser);
            await _db.SaveChangesAsync();
        }

        // Check if buddy relationship already exists
        var existingBuddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.UserId == userId && ab.BuddyUserId == buddyUser.Id);

        if (existingBuddy != null)
        {
            return BadRequest(new { message = "This person is already your accountability buddy" });
        }

        // Create buddy relationship
        var accountabilityBuddy = new AccountabilityBuddy
        {
            UserId = userId,
            BuddyUserId = buddyUser.Id
        };
        _db.AccountabilityBuddies.Add(accountabilityBuddy);

        // Generate invite token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var inviteToken = new BuddyInviteToken
        {
            Token = token,
            InviterUserId = userId,
            InvitedEmail = email,
            BuddyUserId = buddyUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.BuddyInviteTokens.Add(inviteToken);

        await _db.SaveChangesAsync();

        // Build invite URL
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var inviteUrl = $"{frontendUrl}/auth/buddy-invite?token={token}";

        // Send invite email
        var inviterName = inviter.DisplayName ?? inviter.Username;
        await _emailService.SendBuddyInviteAsync(email, inviterName, inviteUrl);

        return Ok(new BuddyResponse(
            accountabilityBuddy.Id,
            buddyUser.Id,
            buddyUser.Email,
            buddyUser.DisplayName ?? buddyUser.Username,
            accountabilityBuddy.CreatedAt
        ));
    }

    /// <summary>
    /// Remove one of my accountability buddies.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveBuddy(Guid id)
    {
        var userId = GetUserId();

        var buddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.Id == id && ab.UserId == userId);

        if (buddy == null)
        {
            return NotFound(new { message = "Buddy relationship not found" });
        }

        _db.AccountabilityBuddies.Remove(buddy);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Leave as someone's accountability buddy (remove myself).
    /// </summary>
    [HttpDelete("leave/{ownerUserId:guid}")]
    public async Task<IActionResult> LeaveBuddy(Guid ownerUserId)
    {
        var userId = GetUserId();

        var buddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.UserId == ownerUserId && ab.BuddyUserId == userId);

        if (buddy == null)
        {
            return NotFound(new { message = "Buddy relationship not found" });
        }

        _db.AccountabilityBuddies.Remove(buddy);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get another user's Today view (as their accountability buddy).
    /// </summary>
    [HttpGet("{targetUserId:guid}/today")]
    public async Task<ActionResult<BuddyTodayViewResponse>> GetBuddyTodayView(Guid targetUserId, [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();

        // Verify buddy relationship exists
        var isBuddy = await _db.AccountabilityBuddies
            .AnyAsync(ab => ab.UserId == targetUserId && ab.BuddyUserId == userId);

        if (!isBuddy)
        {
            return Forbid();
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Get target user info
        var targetUser = await _db.Users.FirstAsync(u => u.Id == targetUserId);

        // Get habit stacks with completions
        var habitStacks = await GetTodayHabitStacks(targetUserId, targetDate);

        // Get upcoming tasks
        var upcomingTasks = await GetUpcomingTasks(targetUserId, targetDate);

        // Get completed tasks
        var completedTasks = await GetCompletedTasks(targetUserId, targetDate);

        // Get identity feedback
        var identityFeedback = await GetIdentityFeedback(targetUserId, targetDate);

        return Ok(new BuddyTodayViewResponse(
            targetUserId,
            targetUser.DisplayName ?? targetUser.Username,
            targetDate,
            habitStacks,
            upcomingTasks,
            completedTasks,
            identityFeedback
        ));
    }

    /// <summary>
    /// Get another user's journal entries (as their accountability buddy).
    /// </summary>
    [HttpGet("{targetUserId:guid}/journal")]
    public async Task<ActionResult<List<BuddyJournalEntryResponse>>> GetBuddyJournal(Guid targetUserId)
    {
        var userId = GetUserId();

        // Verify buddy relationship exists
        var isBuddy = await _db.AccountabilityBuddies
            .AnyAsync(ab => ab.UserId == targetUserId && ab.BuddyUserId == userId);

        if (!isBuddy)
        {
            return Forbid();
        }

        var entries = await _db.JournalEntries
            .Include(j => j.Author)
            .Include(j => j.Images.OrderBy(i => i.SortOrder))
            .Where(j => j.UserId == targetUserId)
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedAt)
            .ToListAsync();

        var response = entries.Select(j => new BuddyJournalEntryResponse(
            j.Id,
            j.Title,
            j.Description,
            j.EntryDate.ToString("yyyy-MM-dd"),
            j.AuthorUserId,
            j.Author != null ? j.Author.DisplayName ?? j.Author.Username : null,
            j.Images.Select(i => new BuddyJournalImageResponse(i.Id, i.FileName, _storage.GetPresignedUrl(i.S3Key), i.SortOrder)).ToList(),
            j.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Write a journal entry for another user (as their accountability buddy).
    /// </summary>
    [HttpPost("{targetUserId:guid}/journal")]
    public async Task<ActionResult<BuddyJournalEntryResponse>> CreateBuddyJournalEntry(
        Guid targetUserId,
        [FromBody] CreateBuddyJournalEntryRequest request)
    {
        var userId = GetUserId();

        // Verify buddy relationship exists
        var isBuddy = await _db.AccountabilityBuddies
            .AnyAsync(ab => ab.UserId == targetUserId && ab.BuddyUserId == userId);

        if (!isBuddy)
        {
            return Forbid();
        }

        var author = await _db.Users.FirstAsync(u => u.Id == userId);
        var targetUser = await _db.Users.FirstAsync(u => u.Id == targetUserId);

        var entry = new JournalEntry
        {
            UserId = targetUserId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EntryDate = string.IsNullOrEmpty(request.EntryDate)
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : DateOnly.Parse(request.EntryDate),
            AuthorUserId = userId
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Send notification email to the target user
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var journalUrl = $"{frontendUrl}/journal";
        var authorName = author.DisplayName ?? author.Username;

        await _emailService.SendBuddyJournalNotificationAsync(
            targetUser.Email,
            authorName,
            entry.Title,
            journalUrl
        );

        return Ok(new BuddyJournalEntryResponse(
            entry.Id,
            entry.Title,
            entry.Description,
            entry.EntryDate.ToString("yyyy-MM-dd"),
            entry.AuthorUserId,
            authorName,
            new List<BuddyJournalImageResponse>(),
            entry.CreatedAt
        ));
    }

    /// <summary>
    /// Upload an image to a buddy's journal entry (only if you authored the entry).
    /// </summary>
    [HttpPost("{targetUserId:guid}/journal/{entryId:guid}/images")]
    public async Task<ActionResult<BuddyJournalImageResponse>> UploadBuddyJournalImage(
        Guid targetUserId,
        Guid entryId,
        IFormFile file)
    {
        var userId = GetUserId();

        // Verify buddy relationship exists
        var isBuddy = await _db.AccountabilityBuddies
            .AnyAsync(ab => ab.UserId == targetUserId && ab.BuddyUserId == userId);

        if (!isBuddy)
        {
            return Forbid();
        }

        // Get the journal entry and verify the current user authored it
        var entry = await _db.JournalEntries
            .Include(j => j.Images)
            .FirstOrDefaultAsync(j => j.Id == entryId && j.UserId == targetUserId);

        if (entry == null)
        {
            return NotFound(new { message = "Journal entry not found" });
        }

        // Only allow uploading images to entries you authored
        if (entry.AuthorUserId != userId)
        {
            return Forbid();
        }

        // Check image count limit
        if (entry.Images.Count >= 5)
        {
            return BadRequest(new { message = "Maximum of 5 images per entry allowed" });
        }

        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP" });
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB
        {
            return BadRequest(new { message = "File size must be less than 5MB" });
        }

        // Upload to storage
        var extension = Path.GetExtension(file.FileName);
        var s3Key = $"journal/{targetUserId}/{entryId}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        await _storage.UploadAsync(stream, s3Key, file.ContentType);

        // Create image record
        var image = new JournalImage
        {
            JournalEntryId = entryId,
            FileName = file.FileName,
            S3Key = s3Key,
            ContentType = file.ContentType,
            SortOrder = entry.Images.Count
        };

        _db.JournalImages.Add(image);
        await _db.SaveChangesAsync();

        return Ok(new BuddyJournalImageResponse(
            image.Id,
            image.FileName,
            _storage.GetPresignedUrl(image.S3Key),
            image.SortOrder
        ));
    }

    // Helper methods copied from TodayController for consistency
    private async Task<List<TodayHabitStackResponse>> GetTodayHabitStacks(Guid userId, DateOnly targetDate)
    {
        var stacks = await _db.HabitStacks
            .Include(s => s.Identity)
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Completions)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return stacks.Select(s => new TodayHabitStackResponse(
            s.Id,
            s.Name,
            s.TriggerCue,
            s.IdentityId,
            s.Identity?.Name,
            s.Identity?.Color,
            s.Items.Select(i => new TodayHabitStackItemResponse(
                i.Id,
                i.HabitDescription,
                i.Completions.Any(c => c.CompletedDate == targetDate),
                i.CurrentStreak
            )),
            s.Items.Count(i => i.Completions.Any(c => c.CompletedDate == targetDate)),
            s.Items.Count
        )).ToList();
    }

    private async Task<List<TodayTaskResponse>> GetUpcomingTasks(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        var tasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status != TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow))
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();

        return tasks.Select(t => new TodayTaskResponse(
            t.Id,
            t.Title,
            t.Description,
            t.GoalId,
            t.Goal.Title,
            t.IdentityId,
            t.Identity?.Name,
            t.Identity?.Icon,
            t.DueDate,
            t.Status.ToString()
        )).ToList();
    }

    private async Task<List<TodayTaskResponse>> GetCompletedTasks(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        var tasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status == TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow) &&
                        t.CompletedAt == targetDate)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync();

        return tasks.Select(t => new TodayTaskResponse(
            t.Id,
            t.Title,
            t.Description,
            t.GoalId,
            t.Goal.Title,
            t.IdentityId,
            t.Identity?.Name,
            t.Identity?.Icon,
            t.DueDate,
            t.Status.ToString()
        )).ToList();
    }

    private async Task<List<TodayIdentityFeedbackResponse>> GetIdentityFeedback(Guid userId, DateOnly targetDate)
    {
        var identities = await _db.Identities
            .Include(i => i.HabitStacks)
                .ThenInclude(hs => hs.Items)
                    .ThenInclude(hsi => hsi.Completions)
            .Include(i => i.Tasks)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        return identities.Select(i =>
        {
            var habitCompletionsToday = i.HabitStacks
                .SelectMany(hs => hs.Items)
                .SelectMany(hsi => hsi.Completions.Where(c => c.CompletedDate == targetDate))
                .Count();

            var taskCompletionsToday = i.Tasks
                .Count(t => t.Status == TaskItemStatus.Completed &&
                           t.CompletedAt.HasValue &&
                           t.CompletedAt.Value == targetDate);

            var totalToday = habitCompletionsToday + taskCompletionsToday;

            return new TodayIdentityFeedbackResponse(
                i.Id,
                i.Name,
                i.Color,
                i.Icon,
                totalToday,
                GenerateReinforcementMessage(i.Name, totalToday)
            );
        })
        .Where(i => i.CompletionsToday > 0)
        .OrderByDescending(i => i.CompletionsToday)
        .ToList();
    }

    private static string GenerateReinforcementMessage(string identityName, int completions) =>
        completions switch
        {
            1 => $"Showed up as {identityName} today!",
            2 => $"Two votes for {identityName}!",
            >= 3 => $"Amazing! {completions} votes for {identityName} today!",
            _ => $"Keep building {identityName}!"
        };

    private async Task<string> GenerateUniqueUsername(string baseName)
    {
        var username = baseName.ToLowerInvariant().Replace(" ", "");
        var exists = await _db.Users.AnyAsync(u => u.Username == username);

        if (!exists)
        {
            return username;
        }

        var suffix = 1;
        while (await _db.Users.AnyAsync(u => u.Username == $"{username}{suffix}"))
        {
            suffix++;
        }

        return $"{username}{suffix}";
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
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
    DateTime CreatedAt
);

public record BuddyJournalImageResponse(
    Guid Id,
    string FileName,
    string Url,
    int SortOrder
);

public record CreateBuddyJournalEntryRequest(
    string Title,
    string? Description,
    string? EntryDate
);
