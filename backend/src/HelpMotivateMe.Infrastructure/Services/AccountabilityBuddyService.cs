using System.Security.Cryptography;
using HelpMotivateMe.Core.DTOs.Buddies;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpMotivateMe.Infrastructure.Services;

/// <summary>
///     Service for managing accountability buddy relationships and buddy-related operations.
/// </summary>
public class AccountabilityBuddyService : IAccountabilityBuddyService
{
    private readonly IQueryInterface<AccountabilityBuddy> _buddies;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IQueryInterface<JournalEntry> _journalEntries;
    private readonly IStorageService _storage;
    private readonly ITodayViewService _todayViewService;

    public AccountabilityBuddyService(
        AppDbContext db,
        IQueryInterface<AccountabilityBuddy> buddies,
        IQueryInterface<JournalEntry> journalEntries,
        IStorageService storage,
        IEmailService emailService,
        IConfiguration configuration,
        ITodayViewService todayViewService)
    {
        _db = db;
        _buddies = buddies;
        _journalEntries = journalEntries;
        _storage = storage;
        _emailService = emailService;
        _configuration = configuration;
        _todayViewService = todayViewService;
    }


    /// <summary>
    ///     Get all buddies for a user (people they've added as buddies).
    /// </summary>
    public async Task<List<BuddyInfo>> GetMyBuddiesAsync(Guid userId)
    {
        return await _buddies
            .Include(ab => ab.BuddyUser)
            .Where(ab => ab.UserId == userId)
            .Select(ab => new BuddyInfo(
                ab.Id,
                ab.BuddyUserId,
                ab.BuddyUser.Email,
                ab.BuddyUser.DisplayName ?? ab.BuddyUser.Email,
                ab.CreatedAt
            ))
            .ToListAsync();
    }

    /// <summary>
    ///     Get all users this person is a buddy for.
    /// </summary>
    public async Task<List<BuddyForInfo>> GetBuddyingForAsync(Guid userId)
    {
        return await _buddies
            .Include(ab => ab.User)
            .Where(ab => ab.BuddyUserId == userId)
            .Select(ab => new BuddyForInfo(
                ab.Id,
                ab.UserId,
                ab.User.Email,
                ab.User.DisplayName ?? ab.User.Email,
                ab.CreatedAt
            ))
            .ToListAsync();
    }

    /// <summary>
    ///     Check if a user is a buddy for another user.
    /// </summary>
    public async Task<bool> IsBuddyForAsync(Guid buddyUserId, Guid targetUserId)
    {
        return await _buddies.AnyAsync(ab => ab.UserId == targetUserId && ab.BuddyUserId == buddyUserId);
    }

    /// <summary>
    ///     Invite a new accountability buddy by email.
    /// </summary>
    public async Task<InviteBuddyResult> InviteBuddyAsync(Guid inviterUserId, string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var inviter = await _db.Users.FirstAsync(u => u.Id == inviterUserId);

        // Check if user is trying to add themselves
        if (inviter.Email.ToLowerInvariant() == normalizedEmail)
            return new InviteBuddyResult(false, "You cannot add yourself as an accountability buddy", null);

        // Find or create the buddy user
        var buddyUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (buddyUser == null)
        {
            buddyUser = new User
            {
                Email = normalizedEmail,
                PreferredLanguage = inviter.PreferredLanguage
            };
            _db.Users.Add(buddyUser);
            await _db.SaveChangesAsync();
        }

        // Check if buddy relationship already exists
        var existingBuddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.UserId == inviterUserId && ab.BuddyUserId == buddyUser.Id);

        if (existingBuddy != null)
            return new InviteBuddyResult(false, "This person is already your accountability buddy", null);

        // Create buddy relationship
        var accountabilityBuddy = new AccountabilityBuddy
        {
            UserId = inviterUserId,
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
            InviterUserId = inviterUserId,
            InvitedEmail = normalizedEmail,
            BuddyUserId = buddyUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.BuddyInviteTokens.Add(inviteToken);

        await _db.SaveChangesAsync();

        // Build invite URL and send email
        var frontendUrl = _configuration["FrontendUrl"] ??
                          _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var inviteUrl = $"{frontendUrl}/auth/buddy-invite?token={token}";
        var inviterName = inviter.DisplayName ?? inviter.Email;
        await _emailService.SendBuddyInviteAsync(normalizedEmail, inviterName, inviteUrl, inviter.PreferredLanguage);

        var buddyInfo = new BuddyInfo(
            accountabilityBuddy.Id,
            buddyUser.Id,
            buddyUser.Email,
            buddyUser.DisplayName ?? buddyUser.Email,
            accountabilityBuddy.CreatedAt
        );

        return new InviteBuddyResult(true, null, buddyInfo);
    }

    /// <summary>
    ///     Remove a buddy relationship (user removes their buddy).
    /// </summary>
    public async Task<bool> RemoveBuddyAsync(Guid userId, Guid buddyRelationshipId)
    {
        var buddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.Id == buddyRelationshipId && ab.UserId == userId);

        if (buddy == null) return false;

        _db.AccountabilityBuddies.Remove(buddy);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    ///     Leave as someone's buddy (buddy removes themselves).
    /// </summary>
    public async Task<bool> LeaveBuddyAsync(Guid buddyUserId, Guid ownerUserId)
    {
        var buddy = await _db.AccountabilityBuddies
            .FirstOrDefaultAsync(ab => ab.UserId == ownerUserId && ab.BuddyUserId == buddyUserId);

        if (buddy == null) return false;

        _db.AccountabilityBuddies.Remove(buddy);
        await _db.SaveChangesAsync();
        return true;
    }


    /// <summary>
    ///     Get the today view for a buddy.
    /// </summary>
    public async Task<BuddyTodayViewData> GetBuddyTodayViewAsync(Guid targetUserId, DateOnly targetDate)
    {
        var targetUser = await _db.Users.FirstAsync(u => u.Id == targetUserId);

        var habitStacks = await _todayViewService.GetTodayHabitStacksAsync(targetUserId, targetDate);
        var upcomingTasks = await _todayViewService.GetUpcomingTasksAsync(targetUserId, targetDate);
        var completedTasks = await _todayViewService.GetCompletedTasksAsync(targetUserId, targetDate);
        var identityFeedback = await _todayViewService.GetIdentityFeedbackAsync(targetUserId, targetDate);

        return new BuddyTodayViewData(
            targetUserId,
            targetUser.DisplayName ?? targetUser.Email,
            targetDate,
            habitStacks,
            upcomingTasks,
            completedTasks,
            identityFeedback
        );
    }


    /// <summary>
    ///     Get journal entries for a buddy.
    /// </summary>
    public async Task<List<BuddyJournalEntryData>> GetBuddyJournalAsync(Guid targetUserId)
    {
        var entries = await _journalEntries
            .Include(j => j.Author)
            .Include(j => j.Images.OrderBy(i => i.SortOrder))
            .Include(j => j.Reactions)
            .ThenInclude(r => r.User)
            .Where(j => j.UserId == targetUserId)
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedAt)
            .ToListAsync();

        return entries.Select(j => new BuddyJournalEntryData(
            j.Id,
            j.Title,
            j.Description,
            j.EntryDate.ToString("yyyy-MM-dd"),
            j.AuthorUserId,
            j.Author != null ? j.Author.DisplayName ?? j.Author.Email : null,
            j.Images.Select(i =>
                new BuddyJournalImageData(i.Id, i.FileName, _storage.GetPresignedUrl(i.S3Key), i.SortOrder)).ToList(),
            j.Reactions.Select(r =>
                    new BuddyJournalReactionData(r.Id, r.Emoji, r.UserId, r.User.DisplayName ?? r.User.Email,
                        r.CreatedAt))
                .ToList(),
            j.CreatedAt
        )).ToList();
    }

    /// <summary>
    ///     Create a journal entry for a buddy.
    /// </summary>
    public async Task<BuddyJournalEntryData> CreateBuddyJournalEntryAsync(
        Guid authorUserId,
        Guid targetUserId,
        string title,
        string? description,
        string? entryDate)
    {
        var author = await _db.Users.FirstAsync(u => u.Id == authorUserId);
        var targetUser = await _db.Users.FirstAsync(u => u.Id == targetUserId);

        var entry = new JournalEntry
        {
            UserId = targetUserId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            EntryDate = string.IsNullOrEmpty(entryDate)
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : DateOnly.Parse(entryDate),
            AuthorUserId = authorUserId
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Send notification email
        var frontendUrl = _configuration["FrontendUrl"] ??
                          _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var journalUrl = $"{frontendUrl}/journal";
        var authorName = author.DisplayName ?? author.Email;

        await _emailService.SendBuddyJournalNotificationAsync(
            targetUser.Email,
            authorName,
            entry.Title,
            journalUrl,
            targetUser.PreferredLanguage
        );

        return new BuddyJournalEntryData(
            entry.Id,
            entry.Title,
            entry.Description,
            entry.EntryDate.ToString("yyyy-MM-dd"),
            entry.AuthorUserId,
            authorName,
            new List<BuddyJournalImageData>(),
            new List<BuddyJournalReactionData>(),
            entry.CreatedAt
        );
    }

    /// <summary>
    ///     Upload an image to a buddy's journal entry.
    /// </summary>
    public async Task<UploadImageResult> UploadBuddyJournalImageAsync(
        Guid authorUserId,
        Guid targetUserId,
        Guid entryId,
        Stream imageStream,
        string fileName,
        string contentType,
        long fileSize)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Images)
            .FirstOrDefaultAsync(j => j.Id == entryId && j.UserId == targetUserId);

        if (entry == null) return new UploadImageResult(false, "Journal entry not found", null);

        if (entry.AuthorUserId != authorUserId) return new UploadImageResult(false, "Forbidden", null);

        if (entry.Images.Count >= 5) return new UploadImageResult(false, "Maximum of 5 images per entry allowed", null);

        var extension = Path.GetExtension(fileName);
        var s3Key = $"journal/{targetUserId}/{entryId}/{Guid.NewGuid()}{extension}";

        await _storage.UploadAsync(imageStream, s3Key, contentType);

        var image = new JournalImage
        {
            JournalEntryId = entryId,
            FileName = fileName,
            S3Key = s3Key,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            SortOrder = entry.Images.Count
        };

        _db.JournalImages.Add(image);
        await _db.SaveChangesAsync();

        var imageData = new BuddyJournalImageData(
            image.Id,
            image.FileName,
            _storage.GetPresignedUrl(image.S3Key),
            image.SortOrder
        );

        return new UploadImageResult(true, null, imageData);
    }

    /// <summary>
    ///     Add a reaction to a buddy's journal entry.
    /// </summary>
    public async Task<AddReactionResult> AddBuddyJournalReactionAsync(
        Guid userId,
        Guid targetUserId,
        Guid entryId,
        string emoji)
    {
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == entryId && j.UserId == targetUserId);

        if (entry == null) return new AddReactionResult(false, "Journal entry not found", null);

        var existingReaction = await _db.JournalReactions
            .FirstOrDefaultAsync(r => r.JournalEntryId == entryId && r.UserId == userId && r.Emoji == emoji);

        if (existingReaction != null) return new AddReactionResult(false, "You have already added this reaction", null);

        var user = await _db.Users.FindAsync(userId);

        var reaction = new JournalReaction
        {
            JournalEntryId = entryId,
            UserId = userId,
            Emoji = emoji
        };

        _db.JournalReactions.Add(reaction);
        await _db.SaveChangesAsync();

        var reactionData = new BuddyJournalReactionData(
            reaction.Id,
            reaction.Emoji,
            reaction.UserId,
            user!.DisplayName ?? user.Email,
            reaction.CreatedAt
        );

        return new AddReactionResult(true, null, reactionData);
    }

    /// <summary>
    ///     Remove a reaction from a buddy's journal entry.
    /// </summary>
    public async Task<bool> RemoveBuddyJournalReactionAsync(Guid userId, Guid entryId, Guid reactionId)
    {
        var reaction = await _db.JournalReactions
            .FirstOrDefaultAsync(r => r.Id == reactionId && r.JournalEntryId == entryId && r.UserId == userId);

        if (reaction == null) return false;

        _db.JournalReactions.Remove(reaction);
        await _db.SaveChangesAsync();
        return true;
    }
}