using HelpMotivateMe.Core.DTOs.DailyCommitment;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public class DailyCommitmentService
{
    private readonly AppDbContext _context;
    private readonly IQueryInterface<DailyIdentityCommitment> _commitments;
    private readonly IQueryInterface<Identity> _identities;
    private readonly IQueryInterface<HabitStackItem> _habitStackItems;
    private readonly IQueryInterface<TaskItem> _tasks;
    private readonly IQueryInterface<NotificationPreferences> _notificationPreferences;
    private readonly IdentityScoreService _identityScoreService;

    public DailyCommitmentService(
        AppDbContext context,
        IQueryInterface<DailyIdentityCommitment> commitments,
        IQueryInterface<Identity> identities,
        IQueryInterface<HabitStackItem> habitStackItems,
        IQueryInterface<TaskItem> tasks,
        IQueryInterface<NotificationPreferences> notificationPreferences,
        IdentityScoreService identityScoreService)
    {
        _context = context;
        _commitments = commitments;
        _identities = identities;
        _habitStackItems = habitStackItems;
        _tasks = tasks;
        _notificationPreferences = notificationPreferences;
        _identityScoreService = identityScoreService;
    }

    /// <summary>
    /// Get the commitment for a specific date. Returns null if none exists.
    /// </summary>
    public async Task<DailyCommitmentResponse?> GetCommitmentAsync(Guid userId, DateOnly date)
    {
        var commitment = await _commitments
            .Include(c => c.Identity)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CommitmentDate == date);

        return commitment == null ? null : MapToResponse(commitment);
    }

    /// <summary>
    /// Get identity options with scores for the commitment flow.
    /// </summary>
    public async Task<CommitmentOptionsResponse> GetIdentityOptionsAsync(Guid userId, DateOnly date)
    {
        // Get identity scores
        var scores = await _identityScoreService.CalculateScoresAsync(userId, date);

        // Get user's default mode preference
        var preferences = await _notificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);
        var defaultMode = preferences?.CommitmentDefaultMode ?? "weakest";

        // Determine recommended identity based on mode
        Guid? recommendedIdentityId = null;
        if (scores.Any())
        {
            recommendedIdentityId = defaultMode == "weakest"
                ? scores.OrderBy(s => s.Score).First().Id
                : scores.OrderByDescending(s => s.Score).First().Id;
        }

        var identityOptions = scores.Select(s => new IdentityOptionResponse(
            s.Id,
            s.Name,
            s.Color,
            s.Icon,
            s.Score,
            s.Id == recommendedIdentityId
        )).ToList();

        return new CommitmentOptionsResponse(identityOptions, recommendedIdentityId, defaultMode);
    }

    /// <summary>
    /// Get action suggestions (habits and tasks) for a specific identity.
    /// </summary>
    public async Task<ActionSuggestionsResponse> GetActionSuggestionsAsync(Guid userId, Guid identityId)
    {
        var suggestions = new List<ActionSuggestion>();

        // Get habit stack items linked to this identity
        var habitItems = await _habitStackItems
            .Include(hsi => hsi.HabitStack)
            .Where(hsi => hsi.HabitStack.UserId == userId &&
                         hsi.HabitStack.IdentityId == identityId &&
                         hsi.HabitStack.IsActive)
            .OrderBy(hsi => hsi.HabitStack.SortOrder)
            .ThenBy(hsi => hsi.SortOrder)
            .Take(5)
            .ToListAsync();

        foreach (var item in habitItems)
        {
            suggestions.Add(new ActionSuggestion(
                item.HabitDescription,
                "habit",
                item.Id,
                null
            ));
        }

        // Get tasks linked to this identity
        var tasks = await _tasks
            .Include(t => t.Goal)
            .Where(t => t.Goal.UserId == userId &&
                       t.IdentityId == identityId &&
                       t.Status != TaskItemStatus.Completed &&
                       !t.Goal.IsCompleted)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .ToListAsync();

        foreach (var task in tasks)
        {
            suggestions.Add(new ActionSuggestion(
                task.Title,
                "task",
                null,
                task.Id
            ));
        }

        return new ActionSuggestionsResponse(suggestions);
    }

    /// <summary>
    /// Create a new daily commitment.
    /// </summary>
    public async Task<DailyCommitmentResponse> CreateCommitmentAsync(Guid userId, CreateDailyCommitmentRequest request)
    {
        var today = await GetUserLocalDateAsync(userId);

        // Check if commitment already exists for today
        var existing = await _context.DailyIdentityCommitments
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CommitmentDate == today);

        if (existing != null)
        {
            throw new InvalidOperationException("A commitment already exists for today.");
        }

        // Verify identity belongs to user
        var identity = await _identities.FirstOrDefaultAsync(i => i.Id == request.IdentityId && i.UserId == userId);
        if (identity == null)
        {
            throw new InvalidOperationException("Invalid identity.");
        }

        var commitment = new DailyIdentityCommitment
        {
            UserId = userId,
            CommitmentDate = today,
            IdentityId = request.IdentityId,
            ActionDescription = request.ActionDescription.Trim(),
            LinkedHabitStackItemId = request.LinkedHabitStackItemId,
            LinkedTaskId = request.LinkedTaskId,
            Status = DailyCommitmentStatus.Committed
        };

        _context.DailyIdentityCommitments.Add(commitment);
        await _context.SaveChangesAsync();

        // Reload with identity for response
        await _context.Entry(commitment).Reference(c => c.Identity).LoadAsync();

        return MapToResponse(commitment);
    }

    /// <summary>
    /// Mark a commitment as completed. Awards +1 bonus vote to the identity score.
    /// </summary>
    public async Task<DailyCommitmentResponse> CompleteCommitmentAsync(Guid userId, Guid commitmentId)
    {
        var commitment = await _context.DailyIdentityCommitments
            .Include(c => c.Identity)
            .FirstOrDefaultAsync(c => c.Id == commitmentId && c.UserId == userId);

        if (commitment == null)
        {
            throw new InvalidOperationException("Commitment not found.");
        }

        if (commitment.Status != DailyCommitmentStatus.Committed)
        {
            throw new InvalidOperationException("Commitment cannot be completed in its current state.");
        }

        commitment.Status = DailyCommitmentStatus.Completed;
        commitment.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(commitment);
    }

    /// <summary>
    /// Dismiss a commitment for the day.
    /// </summary>
    public async Task<DailyCommitmentResponse> DismissCommitmentAsync(Guid userId, Guid commitmentId)
    {
        var commitment = await _context.DailyIdentityCommitments
            .Include(c => c.Identity)
            .FirstOrDefaultAsync(c => c.Id == commitmentId && c.UserId == userId);

        if (commitment == null)
        {
            throw new InvalidOperationException("Commitment not found.");
        }

        if (commitment.Status != DailyCommitmentStatus.Committed)
        {
            throw new InvalidOperationException("Commitment cannot be dismissed in its current state.");
        }

        commitment.Status = DailyCommitmentStatus.Dismissed;
        commitment.DismissedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(commitment);
    }

    /// <summary>
    /// Get yesterday's commitment info (for recovery message display).
    /// </summary>
    public async Task<YesterdayCommitmentResponse> GetYesterdayCommitmentAsync(Guid userId)
    {
        var today = await GetUserLocalDateAsync(userId);
        var yesterday = today.AddDays(-1);

        var commitment = await _commitments
            .Include(c => c.Identity)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CommitmentDate == yesterday);

        if (commitment == null)
        {
            return new YesterdayCommitmentResponse(false, null, null);
        }

        // Check if it was missed (Committed status means it was never completed)
        var wasMissed = commitment.Status == DailyCommitmentStatus.Committed;

        // If missed, update status to Missed
        if (wasMissed)
        {
            var entity = await _context.DailyIdentityCommitments
                .FirstAsync(c => c.Id == commitment.Id);
            entity.Status = DailyCommitmentStatus.Missed;
            await _context.SaveChangesAsync();
        }

        return new YesterdayCommitmentResponse(
            wasMissed,
            wasMissed ? commitment.Identity.Name : null,
            wasMissed ? commitment.ActionDescription : null
        );
    }

    /// <summary>
    /// Check if a habit stack item completion should auto-complete the daily commitment.
    /// </summary>
    public async Task<bool> CheckAndAutoCompleteForHabitAsync(Guid userId, Guid habitStackItemId)
    {
        var today = await GetUserLocalDateAsync(userId);

        var commitment = await _context.DailyIdentityCommitments
            .FirstOrDefaultAsync(c => c.UserId == userId &&
                                     c.CommitmentDate == today &&
                                     c.Status == DailyCommitmentStatus.Committed &&
                                     c.LinkedHabitStackItemId == habitStackItemId);

        if (commitment != null)
        {
            commitment.Status = DailyCommitmentStatus.Completed;
            commitment.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a task completion should auto-complete the daily commitment.
    /// </summary>
    public async Task<bool> CheckAndAutoCompleteForTaskAsync(Guid userId, Guid taskId)
    {
        var today = await GetUserLocalDateAsync(userId);

        var commitment = await _context.DailyIdentityCommitments
            .FirstOrDefaultAsync(c => c.UserId == userId &&
                                     c.CommitmentDate == today &&
                                     c.Status == DailyCommitmentStatus.Committed &&
                                     c.LinkedTaskId == taskId);

        if (commitment != null)
        {
            commitment.Status = DailyCommitmentStatus.Completed;
            commitment.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private static DailyCommitmentResponse MapToResponse(DailyIdentityCommitment commitment)
    {
        return new DailyCommitmentResponse(
            commitment.Id,
            commitment.CommitmentDate,
            commitment.IdentityId,
            commitment.Identity.Name,
            commitment.Identity.Color,
            commitment.Identity.Icon,
            commitment.ActionDescription,
            commitment.LinkedHabitStackItemId,
            commitment.LinkedTaskId,
            commitment.Status,
            commitment.CompletedAt,
            commitment.CreatedAt
        );
    }

    /// <summary>
    /// Resolves the user's current local date based on their timezone settings.
    /// </summary>
    private async Task<DateOnly> GetUserLocalDateAsync(Guid userId)
    {
        var preferences = await _notificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (preferences == null)
        {
            // Fallback to UTC if no preferences set
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var localDateTime = ResolveLocalTime(DateTime.UtcNow, preferences.TimezoneId, preferences.UtcOffsetMinutes);
        return DateOnly.FromDateTime(localDateTime);
    }

    /// <summary>
    /// Resolves the user's local time from UTC using their timezone settings.
    /// </summary>
    private static DateTime ResolveLocalTime(DateTime utcNow, string timezoneId, int utcOffsetMinutes)
    {
        // Try to use TimezoneId first (authoritative)
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
        catch (InvalidTimeZoneException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
    }
}
