using HelpMotivateMe.Core.DTOs.Admin;
using HelpMotivateMe.Core.Enums;
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

    public AdminController(AppDbContext db)
    {
        _db = db;
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
                u.Username.ToLower().Contains(searchLower) ||
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

        // Apply pagination and ordering
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserResponse(
                u.Id,
                u.Username,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.MembershipTier.ToString(),
                u.Role.ToString(),
                u.CreatedAt,
                u.UpdatedAt
            ))
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(users);
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

        return Ok(new AdminUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.MembershipTier.ToString(),
            user.Role.ToString(),
            user.CreatedAt,
            user.UpdatedAt
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

        return Ok(new AdminUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.MembershipTier.ToString(),
            user.Role.ToString(),
            user.CreatedAt,
            user.UpdatedAt
        ));
    }
}

public record UpdateRoleRequest(string Role);
