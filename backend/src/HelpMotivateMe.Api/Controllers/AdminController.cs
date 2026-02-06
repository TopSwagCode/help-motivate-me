using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Admin;
using HelpMotivateMe.Core.DTOs.Waitlist;
using static HelpMotivateMe.Core.DTOs.Admin.AnalyticsOverviewResponse;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IAiBudgetService _aiBudgetService;

    public AdminController(AppDbContext db, IConfiguration configuration, IEmailService emailService, IAiBudgetService aiBudgetService)
    {
        _db = db;
        _configuration = configuration;
        _emailService = emailService;
        _aiBudgetService = aiBudgetService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponse>> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // User stats
        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive);

        // Users logged in today (users who updated their record today - approximation via UpdatedAt)
        var usersLoggedInToday = await _db.Users
            .CountAsync(u => u.UpdatedAt >= today && u.UpdatedAt < tomorrow);

        // Membership tier breakdown
        var membershipStats = new MembershipStats(
            FreeUsers: await _db.Users.CountAsync(u => u.MembershipTier == MembershipTier.Free),
            PlusUsers: await _db.Users.CountAsync(u => u.MembershipTier == MembershipTier.Plus),
            ProUsers: await _db.Users.CountAsync(u => u.MembershipTier == MembershipTier.Pro)
        );

        // Total task stats (all time)
        var totalTasksCreated = await _db.TaskItems.CountAsync();
        var totalTasksCompleted = await _db.TaskItems
            .CountAsync(t => t.CompletedAt != null);

        var taskTotals = new TaskTotals(
            TotalTasksCreated: totalTasksCreated,
            TotalTasksCompleted: totalTasksCompleted
        );

        return Ok(new AdminStatsResponse(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            UsersLoggedInToday: usersLoggedInToday,
            MembershipStats: membershipStats,
            TaskTotals: taskTotals
        ));
    }

    [HttpGet("stats/daily")]
    public async Task<ActionResult<DailyStatsResponse>> GetDailyStats([FromQuery] string? date = null)
    {
        DateOnly targetDate;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            targetDate = parsedDate;
        }
        else
        {
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var targetDateTime = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var nextDay = targetDateTime.AddDays(1);

        // Tasks created on this date
        var tasksCreated = await _db.TaskItems
            .CountAsync(t => t.CreatedAt >= targetDateTime && t.CreatedAt < nextDay);

        // Tasks completed on this date
        var tasksCompleted = await _db.TaskItems
            .CountAsync(t => t.CompletedAt == targetDate);

        // Tasks due on this date
        var tasksDue = await _db.TaskItems
            .CountAsync(t => t.DueDate == targetDate);

        return Ok(new DailyStatsResponse(
            Date: targetDate.ToString("yyyy-MM-dd"),
            TasksCreated: tasksCreated,
            TasksCompleted: tasksCompleted,
            TasksDue: tasksDue
        ));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? tier = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<MembershipTier>(tier, true, out var membershipTier))
        {
            query = query.Where(u => u.MembershipTier == membershipTier);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // Get total count for pagination info
        var totalCount = await query.CountAsync();

        // Get AI usage stats grouped by user
        var aiUsageStats = await _db.AiUsageLogs
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .ToDictionaryAsync(x => x.UserId, x => new { x.CallsCount, x.TotalCost });

        // Apply pagination and ordering
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = users.Select(u =>
        {
            aiUsageStats.TryGetValue(u.Id, out var usage);
            return new AdminUserResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.MembershipTier.ToString(),
                u.Role.ToString(),
                u.CreatedAt,
                u.UpdatedAt,
                usage?.CallsCount ?? 0,
                usage?.TotalCost ?? 0m
            );
        }).ToList();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(result);
    }

    [HttpPatch("users/{userId}/toggle-active")]
    public async Task<ActionResult<AdminUserResponse>> ToggleUserActive(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var aiUsage = await _db.AiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return Ok(new AdminUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.MembershipTier.ToString(),
            user.Role.ToString(),
            user.CreatedAt,
            user.UpdatedAt,
            aiUsage?.CallsCount ?? 0,
            aiUsage?.TotalCost ?? 0m
        ));
    }

    [HttpPatch("users/{userId}/role")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUserRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return BadRequest(new { message = "Invalid role. Must be User or Admin." });
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var aiUsage = await _db.AiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return Ok(new AdminUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.MembershipTier.ToString(),
            user.Role.ToString(),
            user.CreatedAt,
            user.UpdatedAt,
            aiUsage?.CallsCount ?? 0,
            aiUsage?.TotalCost ?? 0m
        ));
    }

    [HttpGet("users/{userId}/activity")]
    public async Task<ActionResult<UserActivityResponse>> GetUserActivity(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var now = DateTime.UtcNow;
        var lastWeekStart = now.AddDays(-7);
        var lastWeekStartDate = DateOnly.FromDateTime(lastWeekStart);

        // Last week stats
        var userGoalIds = await _db.Goals.Where(g => g.UserId == userId).Select(g => g.Id).ToListAsync();
        var userHabitStackIds = await _db.HabitStacks.Where(hs => hs.UserId == userId).Select(hs => hs.Id).ToListAsync();
        var userHabitStackItemIds = await _db.HabitStackItems
            .Where(hsi => userHabitStackIds.Contains(hsi.HabitStackId))
            .Select(hsi => hsi.Id)
            .ToListAsync();

        var tasksCreatedLastWeek = await _db.TaskItems
            .Where(t => userGoalIds.Contains(t.GoalId) && t.CreatedAt >= lastWeekStart)
            .CountAsync();
        var tasksCompletedLastWeek = await _db.TaskItems
            .Where(t => userGoalIds.Contains(t.GoalId) && t.CompletedAt >= lastWeekStartDate)
            .CountAsync();
        var goalsCreatedLastWeek = await _db.Goals.CountAsync(g => g.UserId == userId && g.CreatedAt >= lastWeekStart);
        var identitiesCreatedLastWeek = await _db.Identities.CountAsync(i => i.UserId == userId && i.CreatedAt >= lastWeekStart);
        var habitStacksCreatedLastWeek = await _db.HabitStacks.CountAsync(hs => hs.UserId == userId && hs.CreatedAt >= lastWeekStart);
        var habitCompletionsLastWeek = await _db.HabitStackItemCompletions
            .Where(hc => userHabitStackItemIds.Contains(hc.HabitStackItemId) && hc.CompletedAt >= lastWeekStart)
            .CountAsync();
        var journalEntriesLastWeek = await _db.JournalEntries.CountAsync(j => j.UserId == userId && j.CreatedAt >= lastWeekStart);
        
        var aiStatsLastWeek = await _db.AiUsageLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= lastWeekStart)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        // Total (all time) stats
        var tasksCreatedTotal = await _db.TaskItems
            .Where(t => userGoalIds.Contains(t.GoalId))
            .CountAsync();
        var tasksCompletedTotal = await _db.TaskItems
            .Where(t => userGoalIds.Contains(t.GoalId) && t.CompletedAt != null)
            .CountAsync();
        var goalsCreatedTotal = await _db.Goals.CountAsync(g => g.UserId == userId);
        var identitiesCreatedTotal = await _db.Identities.CountAsync(i => i.UserId == userId);
        var habitStacksCreatedTotal = await _db.HabitStacks.CountAsync(hs => hs.UserId == userId);
        var habitCompletionsTotal = await _db.HabitStackItemCompletions
            .Where(hc => userHabitStackItemIds.Contains(hc.HabitStackItemId))
            .CountAsync();
        var journalEntriesTotal = await _db.JournalEntries.CountAsync(j => j.UserId == userId);
        
        var aiStatsTotal = await _db.AiUsageLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.UserId)
            .Select(g => new { CallsCount = g.Count(), TotalCost = g.Sum(a => a.EstimatedCostUsd) })
            .FirstOrDefaultAsync();

        return Ok(new UserActivityResponse(
            user.Id,
            user.Email,
            LastWeek: new UserActivityPeriod(
                tasksCreatedLastWeek,
                tasksCompletedLastWeek,
                goalsCreatedLastWeek,
                identitiesCreatedLastWeek,
                habitStacksCreatedLastWeek,
                habitCompletionsLastWeek,
                journalEntriesLastWeek,
                aiStatsLastWeek?.CallsCount ?? 0,
                aiStatsLastWeek?.TotalCost ?? 0m
            ),
            Total: new UserActivityPeriod(
                tasksCreatedTotal,
                tasksCompletedTotal,
                goalsCreatedTotal,
                identitiesCreatedTotal,
                habitStacksCreatedTotal,
                habitCompletionsTotal,
                journalEntriesTotal,
                aiStatsTotal?.CallsCount ?? 0,
                aiStatsTotal?.TotalCost ?? 0m
            )
        ));
    }

    // ==================== Settings ====================

    [HttpGet("settings")]
    public ActionResult<SignupSettingsResponse> GetSettings()
    {
        var allowSignups = !bool.TryParse(_configuration["Auth:AllowSignups"], out var allowed) || allowed;
        return Ok(new SignupSettingsResponse(allowSignups));
    }

    // ==================== Waitlist Management ====================

    [HttpGet("waitlist")]
    public async Task<ActionResult<IEnumerable<WaitlistEntryResponse>>> GetWaitlist(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.WaitlistEntries.AsQueryable();

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

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(entries);
    }

    [HttpDelete("waitlist/{id}")]
    public async Task<IActionResult> RemoveFromWaitlist(Guid id)
    {
        var entry = await _db.WaitlistEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound(new { message = "Waitlist entry not found" });
        }

        _db.WaitlistEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("waitlist/{id}/approve")]
    public async Task<ActionResult<WhitelistEntryResponse>> ApproveWaitlistEntry(Guid id)
    {
        var waitlistEntry = await _db.WaitlistEntries.FindAsync(id);
        if (waitlistEntry == null)
        {
            return NotFound(new { message = "Waitlist entry not found" });
        }

        // Check if already on whitelist
        var existingWhitelist = await _db.WhitelistEntries
            .FirstOrDefaultAsync(w => w.Email.ToLower() == waitlistEntry.Email.ToLower());

        if (existingWhitelist != null)
        {
            // Remove from waitlist and return existing whitelist entry
            _db.WaitlistEntries.Remove(waitlistEntry);
            await _db.SaveChangesAsync();

            var addedByUser = existingWhitelist.AddedByUserId.HasValue
                ? await _db.Users.FindAsync(existingWhitelist.AddedByUserId.Value)
                : null;

            return Ok(new WhitelistEntryResponse(
                existingWhitelist.Id,
                existingWhitelist.Email,
                existingWhitelist.AddedAt,
                addedByUser?.Email,
                existingWhitelist.InvitedAt
            ));
        }

        var currentUserId = GetUserId();

        // Create whitelist entry
        var whitelistEntry = new WhitelistEntry
        {
            Email = waitlistEntry.Email,
            AddedByUserId = currentUserId,
            InvitedAt = DateTime.UtcNow
        };

        _db.WhitelistEntries.Add(whitelistEntry);
        _db.WaitlistEntries.Remove(waitlistEntry);
        await _db.SaveChangesAsync();

        // Send invite email (default to English for non-registered users)
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendUrl}/auth/login";
        await _emailService.SendWhitelistInviteAsync(waitlistEntry.Email, loginUrl, Language.English);

        var currentUser = currentUserId.HasValue
            ? await _db.Users.FindAsync(currentUserId.Value)
            : null;

        return Ok(new WhitelistEntryResponse(
            whitelistEntry.Id,
            whitelistEntry.Email,
            whitelistEntry.AddedAt,
            currentUser?.Email,
            whitelistEntry.InvitedAt
        ));
    }

    // ==================== Whitelist Management ====================

    [HttpGet("whitelist")]
    public async Task<ActionResult<IEnumerable<WhitelistEntryResponse>>> GetWhitelist(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.WhitelistEntries
            .Include(w => w.AddedByUser)
            .AsQueryable();

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
                w.Id,
                w.Email,
                w.AddedAt,
                w.AddedByUser != null ? w.AddedByUser.Email : null,
                w.InvitedAt
            ))
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(entries);
    }

    [HttpPost("whitelist")]
    public async Task<ActionResult<WhitelistEntryResponse>> AddToWhitelist([FromBody] InviteUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        var email = request.Email.ToLowerInvariant().Trim();

        // Check if already on whitelist
        var existingEntry = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.Email.ToLower() == email);
        if (existingEntry != null)
        {
            return BadRequest(new { message = "Email is already on the whitelist" });
        }

        var currentUserId = GetUserId();

        var entry = new WhitelistEntry
        {
            Email = email,
            AddedByUserId = currentUserId,
            InvitedAt = DateTime.UtcNow
        };

        _db.WhitelistEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Send invite email (default to English for non-registered users)
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
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

        return Ok(new WhitelistEntryResponse(
            entry.Id,
            entry.Email,
            entry.AddedAt,
            currentUser?.Email,
            entry.InvitedAt
        ));
    }

    [HttpDelete("whitelist/{id}")]
    public async Task<IActionResult> RemoveFromWhitelist(Guid id)
    {
        var entry = await _db.WhitelistEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound(new { message = "Whitelist entry not found" });
        }

        _db.WhitelistEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==================== Analytics Events ====================

    [HttpGet("analytics/overview")]
    public async Task<ActionResult<AnalyticsOverviewResponse>> GetAnalyticsOverview([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        // Total events in period
        var totalEvents = await _db.AnalyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .CountAsync();

        // Unique users
        var uniqueUsers = await _db.AnalyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync();

        // Unique sessions
        var uniqueSessions = await _db.AnalyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .Select(e => e.SessionId)
            .Distinct()
            .CountAsync();

        // Average events per session
        var avgEventsPerSession = uniqueSessions > 0 ? (double)totalEvents / uniqueSessions : 0;

        // Top event types
        var topEventTypesRaw = await _db.AnalyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var topEventTypes = topEventTypesRaw
            .Select(x => new EventTypeCount(x.EventType, x.Count))
            .ToList();

        // Daily event counts
        var dailyEventCounts = await _db.AnalyticsEvents
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dailyEvents = dailyEventCounts
            .Select(x => new DailyEventCount(x.Date.ToString("yyyy-MM-dd"), x.Count))
            .ToList();

        // Recent sessions with duration
        var recentSessionsRaw = await _db.AnalyticsEvents
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

        // Fetch usernames
        var userIds = recentSessionsRaw.Select(s => s.UserId).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        var recentSessions = recentSessionsRaw.Select(s => new SessionSummary(
            s.SessionId,
            s.UserId,
            users.GetValueOrDefault(s.UserId, "Unknown"),
            s.FirstEvent,
            s.LastEvent,
            s.EventCount,
            Math.Round((s.LastEvent - s.FirstEvent).TotalMinutes, 1)
        )).ToList();

        return Ok(new AnalyticsOverviewResponse(
            totalEvents,
            uniqueUsers,
            uniqueSessions,
            Math.Round(avgEventsPerSession, 2),
            topEventTypes,
            dailyEvents,
            recentSessions
        ));
    }

    // ==================== AI Usage ====================

    [HttpGet("ai-usage/stats")]
    public async Task<ActionResult<AiUsageStatsResponse>> GetAiUsageStats()
    {
        var stats = await _aiBudgetService.GetBudgetStatsAsync();
        return Ok(new AiUsageStatsResponse(
            stats.TotalEstimatedAllTime,
            stats.TotalActualAllTime,
            stats.TotalEstimatedLast30Days,
            stats.TotalActualLast30Days,
            stats.GlobalLimitLast30DaysUsd,
            stats.PerUserLimitLast30DaysUsd
        ));
    }

    [HttpGet("ai-usage")]
    public async Task<ActionResult<PaginatedResponse<AiUsageLogResponse>>> GetAiUsageLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var totalCount = await _db.AiUsageLogs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await _db.AiUsageLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AiUsageLogResponse(
                l.Id,
                l.UserId,
                l.User.Email,
                l.Model,
                l.InputTokens,
                l.OutputTokens,
                l.AudioDurationSeconds,
                l.EstimatedCostUsd,
                l.ActualCostUsd,
                l.RequestType,
                l.Rejected,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<AiUsageLogResponse>(
            logs,
            totalCount,
            page,
            pageSize,
            totalPages
        ));
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public record UpdateRoleRequest(string Role);
