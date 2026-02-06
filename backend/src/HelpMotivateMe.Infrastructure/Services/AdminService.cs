using HelpMotivateMe.Core.DTOs.Admin;
using HelpMotivateMe.Core.DTOs.Waitlist;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpMotivateMe.Infrastructure.Services;

/// <summary>
///     Service for admin operations including user management, waitlist/whitelist,
///     analytics, and AI usage tracking.
/// </summary>
public class AdminService : IAdminService
{
    private readonly IAiBudgetService _aiBudgetService;
    private readonly IQueryInterface<AiUsageLog> _aiUsageLogs;
    private readonly IQueryInterface<AnalyticsEvent> _analyticsEvents;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IQueryInterface<Goal> _goals;
    private readonly IQueryInterface<HabitStackItemCompletion> _habitCompletions;
    private readonly IQueryInterface<HabitStack> _habitStacks;
    private readonly IQueryInterface<Identity> _identitiesQuery;
    private readonly IQueryInterface<JournalEntry> _journalEntries;
    private readonly IQueryInterface<TaskItem> _taskItems;
    private readonly IQueryInterface<User> _users;
    private readonly IQueryInterface<WaitlistEntry> _waitlistEntries;
    private readonly IQueryInterface<WhitelistEntry> _whitelistEntries;

    public AdminService(
        AppDbContext db,
        IQueryInterface<User> users,
        IQueryInterface<TaskItem> taskItems,
        IQueryInterface<AiUsageLog> aiUsageLogs,
        IQueryInterface<WaitlistEntry> waitlistEntries,
        IQueryInterface<WhitelistEntry> whitelistEntries,
        IQueryInterface<AnalyticsEvent> analyticsEvents,
        IQueryInterface<Goal> goals,
        IQueryInterface<Identity> identitiesQuery,
        IQueryInterface<HabitStack> habitStacks,
        IQueryInterface<HabitStackItemCompletion> habitCompletions,
        IQueryInterface<JournalEntry> journalEntries,
        IConfiguration configuration,
        IEmailService emailService,
        IAiBudgetService aiBudgetService)
    {
        _db = db;
        _users = users;
        _taskItems = taskItems;
        _aiUsageLogs = aiUsageLogs;
        _waitlistEntries = waitlistEntries;
        _whitelistEntries = whitelistEntries;
        _analyticsEvents = analyticsEvents;
        _goals = goals;
        _identitiesQuery = identitiesQuery;
        _habitStacks = habitStacks;
        _habitCompletions = habitCompletions;
        _journalEntries = journalEntries;
        _configuration = configuration;
        _emailService = emailService;
        _aiBudgetService = aiBudgetService;
    }

    // ==================== Stats ====================

    public async Task<AdminStatsResponse> GetStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalUsers = await _users.CountAsync();
        var activeUsers = await _users.CountAsync(u => u.IsActive);
        var usersLoggedInToday = await _users
            .CountAsync(u => u.UpdatedAt >= today && u.UpdatedAt < tomorrow);

        var membershipStats = new MembershipStats(
            await _users.CountAsync(u => u.MembershipTier == MembershipTier.Free),
            await _users.CountAsync(u => u.MembershipTier == MembershipTier.Plus),
            await _users.CountAsync(u => u.MembershipTier == MembershipTier.Pro)
        );

        var totalTasksCreated = await _taskItems.CountAsync();
        var totalTasksCompleted = await _taskItems.CountAsync(t => t.CompletedAt != null);

        var taskTotals = new TaskTotals(
            totalTasksCreated,
            totalTasksCompleted
        );

        return new AdminStatsResponse(
            totalUsers,
            activeUsers,
            usersLoggedInToday,
            membershipStats,
            taskTotals
        );
    }

    public async Task<DailyStatsResponse> GetDailyStatsAsync(DateOnly targetDate)
    {
        var targetDateTime = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var nextDay = targetDateTime.AddDays(1);

        var tasksCreated = await _taskItems
            .CountAsync(t => t.CreatedAt >= targetDateTime && t.CreatedAt < nextDay);
        var tasksCompleted = await _taskItems
            .CountAsync(t => t.CompletedAt == targetDate);
        var tasksDue = await _taskItems
            .CountAsync(t => t.DueDate == targetDate);

        return new DailyStatsResponse(
            targetDate.ToString("yyyy-MM-dd"),
            tasksCreated,
            tasksCompleted,
            tasksDue
        );
    }

    // ==================== Users ====================

    public async Task<(List<AdminUserResponse> Users, int TotalCount)> GetUsersAsync(
        string? search, string? tier, bool? isActive, int page, int pageSize)
    {
        var query = _users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<MembershipTier>(tier, true, out var membershipTier))
            query = query.Where(u => u.MembershipTier == membershipTier);

        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var aiUsageStats = await _aiUsageLogs
            .Where(a => userIds.Contains(a.UserId))
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .ToDictionaryAsync(x => x.UserId, x => new { x.CallsCount, x.TotalCost });

        var result = users.Select(u =>
        {
            aiUsageStats.TryGetValue(u.Id, out var usage);
            return new AdminUserResponse(
                u.Id, u.Email, u.DisplayName, u.IsActive,
                u.MembershipTier.ToString(), u.Role.ToString(),
                u.CreatedAt, u.UpdatedAt,
                usage?.CallsCount ?? 0, usage?.TotalCost ?? 0m
            );
        }).ToList();

        return (result, totalCount);
    }

    public async Task<AdminUserResponse?> ToggleUserActiveAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var aiUsage = await _db.AiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return new AdminUserResponse(
            user.Id, user.Email, user.DisplayName, user.IsActive,
            user.MembershipTier.ToString(), user.Role.ToString(),
            user.CreatedAt, user.UpdatedAt,
            aiUsage?.CallsCount ?? 0, aiUsage?.TotalCost ?? 0m
        );
    }

    public async Task<AdminUserResponse?> UpdateUserRoleAsync(Guid userId, string role)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return null;

        user.Role = parsedRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var aiUsage = await _db.AiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return new AdminUserResponse(
            user.Id, user.Email, user.DisplayName, user.IsActive,
            user.MembershipTier.ToString(), user.Role.ToString(),
            user.CreatedAt, user.UpdatedAt,
            aiUsage?.CallsCount ?? 0, aiUsage?.TotalCost ?? 0m
        );
    }

    public async Task<UserActivityResponse?> GetUserActivityAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var now = DateTime.UtcNow;
        var lastWeekStart = now.AddDays(-7);
        var lastWeekStartDate = DateOnly.FromDateTime(lastWeekStart);

        // Last week stats
        var tasksCreatedLastWeek = await _taskItems
            .Where(t => t.Goal.UserId == userId && t.CreatedAt >= lastWeekStart).CountAsync();
        var tasksCompletedLastWeek = await _taskItems
            .Where(t => t.Goal.UserId == userId && t.CompletedAt >= lastWeekStartDate).CountAsync();
        var goalsCreatedLastWeek = await _goals.CountAsync(g => g.UserId == userId && g.CreatedAt >= lastWeekStart);
        var identitiesCreatedLastWeek =
            await _identitiesQuery.CountAsync(i => i.UserId == userId && i.CreatedAt >= lastWeekStart);
        var habitStacksCreatedLastWeek =
            await _habitStacks.CountAsync(hs => hs.UserId == userId && hs.CreatedAt >= lastWeekStart);
        var habitCompletionsLastWeek = await _habitCompletions
            .Where(hc => hc.HabitStackItem.HabitStack.UserId == userId && hc.CompletedAt >= lastWeekStart).CountAsync();
        var journalEntriesLastWeek =
            await _journalEntries.CountAsync(j => j.UserId == userId && j.CreatedAt >= lastWeekStart);
        var aiStatsLastWeek = await _aiUsageLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= lastWeekStart)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        // Total (all time) stats
        var tasksCreatedTotal = await _taskItems.Where(t => t.Goal.UserId == userId).CountAsync();
        var tasksCompletedTotal =
            await _taskItems.Where(t => t.Goal.UserId == userId && t.CompletedAt != null).CountAsync();
        var goalsCreatedTotal = await _goals.CountAsync(g => g.UserId == userId);
        var identitiesCreatedTotal = await _identitiesQuery.CountAsync(i => i.UserId == userId);
        var habitStacksCreatedTotal = await _habitStacks.CountAsync(hs => hs.UserId == userId);
        var habitCompletionsTotal =
            await _habitCompletions.Where(hc => hc.HabitStackItem.HabitStack.UserId == userId).CountAsync();
        var journalEntriesTotal = await _journalEntries.CountAsync(j => j.UserId == userId);
        var aiStatsTotal = await _aiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return new UserActivityResponse(
            user.Id, user.Email,
            new UserActivityPeriod(
                tasksCreatedLastWeek, tasksCompletedLastWeek, goalsCreatedLastWeek,
                identitiesCreatedLastWeek, habitStacksCreatedLastWeek, habitCompletionsLastWeek,
                journalEntriesLastWeek, aiStatsLastWeek?.CallsCount ?? 0, aiStatsLastWeek?.TotalCost ?? 0m
            ),
            new UserActivityPeriod(
                tasksCreatedTotal, tasksCompletedTotal, goalsCreatedTotal,
                identitiesCreatedTotal, habitStacksCreatedTotal, habitCompletionsTotal,
                journalEntriesTotal, aiStatsTotal?.CallsCount ?? 0, aiStatsTotal?.TotalCost ?? 0m
            )
        );
    }

    // ==================== Settings ====================

    public bool GetAllowSignups()
    {
        return !bool.TryParse(_configuration["Auth:AllowSignups"], out var allowed) || allowed;
    }

    // ==================== Waitlist ====================

    public async Task<(List<WaitlistEntryResponse> Entries, int TotalCount)> GetWaitlistAsync(
        string? search, int page, int pageSize)
    {
        var query = _waitlistEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(w =>
                w.Email.ToLower().Contains(searchLower) ||
                w.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WaitlistEntryResponse(w.Id, w.Email, w.Name, w.CreatedAt))
            .ToListAsync();

        return (entries, totalCount);
    }

    public async Task<bool> RemoveFromWaitlistAsync(Guid id)
    {
        var entry = await _db.WaitlistEntries.FindAsync(id);
        if (entry == null) return false;

        _db.WaitlistEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WhitelistEntryResponse?> ApproveWaitlistEntryAsync(Guid id, Guid? currentUserId)
    {
        var waitlistEntry = await _db.WaitlistEntries.FindAsync(id);
        if (waitlistEntry == null) return null;

        // Check if already on whitelist
        var existingWhitelist = await _db.WhitelistEntries
            .FirstOrDefaultAsync(w => w.Email.ToLower() == waitlistEntry.Email.ToLower());

        if (existingWhitelist != null)
        {
            _db.WaitlistEntries.Remove(waitlistEntry);
            await _db.SaveChangesAsync();

            var addedByUser = existingWhitelist.AddedByUserId.HasValue
                ? await _db.Users.FindAsync(existingWhitelist.AddedByUserId.Value)
                : null;

            return new WhitelistEntryResponse(
                existingWhitelist.Id, existingWhitelist.Email, existingWhitelist.AddedAt,
                addedByUser?.Email, existingWhitelist.InvitedAt
            );
        }

        var whitelistEntry = new WhitelistEntry
        {
            Email = waitlistEntry.Email,
            AddedByUserId = currentUserId,
            InvitedAt = DateTime.UtcNow
        };

        _db.WhitelistEntries.Add(whitelistEntry);
        _db.WaitlistEntries.Remove(waitlistEntry);
        await _db.SaveChangesAsync();

        // Send invite email
        var frontendUrl = _configuration["FrontendUrl"] ??
                          _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendUrl}/auth/login";
        await _emailService.SendWhitelistInviteAsync(waitlistEntry.Email, loginUrl, Language.English);

        var currentUser = currentUserId.HasValue
            ? await _db.Users.FindAsync(currentUserId.Value)
            : null;

        return new WhitelistEntryResponse(
            whitelistEntry.Id, whitelistEntry.Email, whitelistEntry.AddedAt,
            currentUser?.Email, whitelistEntry.InvitedAt
        );
    }

    // ==================== Whitelist ====================

    public async Task<(List<WhitelistEntryResponse> Entries, int TotalCount)> GetWhitelistAsync(
        string? search, int page, int pageSize)
    {
        var query = _whitelistEntries.Include(w => w.AddedByUser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(w => w.Email.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(w => w.AddedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WhitelistEntryResponse(
                w.Id, w.Email, w.AddedAt,
                w.AddedByUser != null ? w.AddedByUser.Email : null,
                w.InvitedAt
            ))
            .ToListAsync();

        return (entries, totalCount);
    }

    public async Task<(WhitelistEntryResponse? Entry, string? Error)> AddToWhitelistAsync(
        string email, Guid? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (null, "Email is required");

        email = email.ToLowerInvariant().Trim();

        var existingEntry = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.Email.ToLower() == email);
        if (existingEntry != null)
            return (null, "Email is already on the whitelist");

        var entry = new WhitelistEntry
        {
            Email = email,
            AddedByUserId = currentUserId,
            InvitedAt = DateTime.UtcNow
        };

        _db.WhitelistEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Send invite email
        var frontendUrl = _configuration["FrontendUrl"] ??
                          _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendUrl}/auth/login";
        await _emailService.SendWhitelistInviteAsync(email, loginUrl, Language.English);

        // Remove from waitlist if exists
        var waitlistEntry = await _db.WaitlistEntries.FirstOrDefaultAsync(w => w.Email.ToLower() == email);
        if (waitlistEntry != null)
        {
            _db.WaitlistEntries.Remove(waitlistEntry);
            await _db.SaveChangesAsync();
        }

        var currentUser = currentUserId.HasValue
            ? await _db.Users.FindAsync(currentUserId.Value)
            : null;

        return (new WhitelistEntryResponse(
            entry.Id, entry.Email, entry.AddedAt,
            currentUser?.Email, entry.InvitedAt
        ), null);
    }

    public async Task<bool> RemoveFromWhitelistAsync(Guid id)
    {
        var entry = await _db.WhitelistEntries.FindAsync(id);
        if (entry == null) return false;

        _db.WhitelistEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    // ==================== Analytics ====================

    public async Task<AnalyticsOverviewResponse> GetAnalyticsOverviewAsync(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var totalEvents = await _analyticsEvents.Where(e => e.CreatedAt >= startDate).CountAsync();
        var uniqueUsers = await _analyticsEvents.Where(e => e.CreatedAt >= startDate)
            .Select(e => e.UserId).Distinct().CountAsync();
        var uniqueSessions = await _analyticsEvents.Where(e => e.CreatedAt >= startDate)
            .Select(e => e.SessionId).Distinct().CountAsync();

        var avgEventsPerSession = uniqueSessions > 0 ? (double)totalEvents / uniqueSessions : 0;

        var topEventTypesRaw = await _analyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var topEventTypes = topEventTypesRaw.Select(x => new EventTypeCount(x.EventType, x.Count)).ToList();

        var dailyEventCounts = await _analyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dailyEvents = dailyEventCounts.Select(x => new DailyEventCount(x.Date.ToString("yyyy-MM-dd"), x.Count))
            .ToList();

        var recentSessionsRaw = await _analyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => new { e.SessionId, e.UserId })
            .Select(g => new
            {
                g.Key.SessionId,
                g.Key.UserId,
                FirstEvent = g.Min(e => e.CreatedAt),
                LastEvent = g.Max(e => e.CreatedAt),
                EventCount = g.Count()
            })
            .OrderByDescending(x => x.LastEvent)
            .Take(20)
            .ToListAsync();

        var userIds = recentSessionsRaw.Select(s => s.UserId).Distinct().ToList();
        var users = await _users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        var recentSessions = recentSessionsRaw.Select(s => new SessionSummary(
            s.SessionId, s.UserId, users.GetValueOrDefault(s.UserId, "Unknown"),
            s.FirstEvent, s.LastEvent, s.EventCount,
            Math.Round((s.LastEvent - s.FirstEvent).TotalMinutes, 1)
        )).ToList();

        return new AnalyticsOverviewResponse(
            totalEvents, uniqueUsers, uniqueSessions,
            Math.Round(avgEventsPerSession, 2),
            topEventTypes, dailyEvents, recentSessions
        );
    }

    // ==================== AI Usage ====================

    public async Task<AiUsageStatsResponse> GetAiUsageStatsAsync()
    {
        var stats = await _aiBudgetService.GetBudgetStatsAsync();
        return new AiUsageStatsResponse(
            stats.TotalEstimatedAllTime,
            stats.TotalActualAllTime,
            stats.TotalEstimatedLast30Days,
            stats.TotalActualLast30Days,
            stats.GlobalLimitLast30DaysUsd,
            stats.PerUserLimitLast30DaysUsd
        );
    }

    public async Task<PaginatedResponse<AiUsageLogResponse>> GetAiUsageLogsAsync(int page, int pageSize)
    {
        var totalCount = await _aiUsageLogs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await _aiUsageLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AiUsageLogResponse(
                l.Id, l.UserId, l.User.Email, l.Model,
                l.InputTokens, l.OutputTokens, l.AudioDurationSeconds,
                l.EstimatedCostUsd, l.ActualCostUsd, l.RequestType,
                l.Rejected, l.CreatedAt
            ))
            .ToListAsync();

        return new PaginatedResponse<AiUsageLogResponse>(logs, totalCount, page, pageSize, totalPages);
    }
}