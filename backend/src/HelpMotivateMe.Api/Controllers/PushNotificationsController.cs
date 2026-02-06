using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/notifications/push")]
[Authorize]
public class PushNotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPushNotificationService _pushService;

    public PushNotificationsController(AppDbContext db, IPushNotificationService pushService)
    {
        _db = db;
        _pushService = pushService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    /// <summary>
    /// Subscribe to push notifications
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<ActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        var userId = GetUserId();

        // Check if this subscription already exists
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

        if (existing != null)
        {
            // Update existing subscription
            existing.P256dh = request.Keys.P256dh;
            existing.Auth = request.Keys.Auth;
            existing.UserAgent = Request.Headers.UserAgent.ToString();
        }
        else
        {
            // Create new subscription
            var subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.Keys.P256dh,
                Auth = request.Keys.Auth,
                UserAgent = Request.Headers.UserAgent.ToString()
            };
            _db.PushSubscriptions.Add(subscription);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Subscribed to push notifications" });
    }

    /// <summary>
    /// Unsubscribe from push notifications
    /// </summary>
    [HttpDelete("unsubscribe")]
    public async Task<ActionResult> Unsubscribe([FromQuery] string? endpoint = null)
    {
        var userId = GetUserId();

        if (!string.IsNullOrEmpty(endpoint))
        {
            // Remove specific subscription
            var subscription = await _db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

            if (subscription != null)
            {
                _db.PushSubscriptions.Remove(subscription);
            }
        }
        else
        {
            // Remove all subscriptions for user
            var subscriptions = await _db.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            _db.PushSubscriptions.RemoveRange(subscriptions);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Unsubscribed from push notifications" });
    }

    /// <summary>
    /// Get current user's push subscription status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<PushSubscriptionsStatusResponse>> GetStatus()
    {
        var userId = GetUserId();
        var subscriptions = await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .Select(s => new PushSubscriptionStatusResponse(
                s.Id,
                s.CreatedAt,
                s.LastUsedAt,
                s.UserAgent ?? "Unknown"
            ))
            .ToListAsync();

        var response = new PushSubscriptionsStatusResponse(
            subscriptions.Count > 0,
            subscriptions.Count,
            subscriptions
        );

        return Ok(response);
    }

    // ==================== Admin Endpoints ====================

    /// <summary>
    /// Send push notification to a specific user (Admin only)
    /// </summary>
    [HttpPost("admin/send-to-user")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PushNotificationResult>> SendToUser([FromBody] SendPushToUserRequest request)
    {
        var result = await _pushService.SendToUserAsync(request.UserId, request.Title, request.Body, request.Url);
        return Ok(result);
    }

    /// <summary>
    /// Send push notification to all subscribed users (Admin only)
    /// </summary>
    [HttpPost("admin/send-to-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PushNotificationResult>> SendToAll([FromBody] SendPushToAllRequest request)
    {
        var result = await _pushService.SendToAllAsync(request.Title, request.Body, request.Url);
        return Ok(result);
    }

    /// <summary>
    /// Get list of users with push notification status (Admin only)
    /// </summary>
    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserPushStatus>>> GetUsersWithPushStatus(
        [FromQuery] bool? hasPush = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Users
            .Include(u => u.PushSubscriptions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower));
        }

        var users = await query
            .Select(u => new UserPushStatus(
                u.Id,
                u.Email,
                u.PushSubscriptions.Count > 0,
                u.PushSubscriptions.Count,
                u.PushSubscriptions.Max(s => s.LastUsedAt)
            ))
            .ToListAsync();

        if (hasPush.HasValue)
        {
            users = users.Where(u => u.HasPushEnabled == hasPush.Value).ToList();
        }

        return Ok(users);
    }

    /// <summary>
    /// Get push notification stats (Admin only)
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PushNotificationStatsResponse>> GetPushStats()
    {
        var totalSubscriptions = await _db.PushSubscriptions.CountAsync();
        var usersWithPush = await _db.PushSubscriptions
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();
        
        var oldestSubscription = await _db.PushSubscriptions
            .OrderBy(s => s.CreatedAt)
            .Select(s => (DateTime?)s.CreatedAt)
            .FirstOrDefaultAsync();
            
        var newestSubscription = await _db.PushSubscriptions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => (DateTime?)s.CreatedAt)
            .FirstOrDefaultAsync();

        var response = new PushNotificationStatsResponse(
            totalSubscriptions,
            usersWithPush,
            oldestSubscription,
            newestSubscription
        );

        return Ok(response);
    }
}
