using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/notifications/push")]
public sealed class PushNotificationsController(
    AppDbContext db,
    IQueryInterface<PushSubscription> subscriptions,
    IResourceAuthorizationService authorization,
    IPushNotificationService pushNotifications,
    VapidKeyMaterial keyMaterial) : ControllerBase
{
    [HttpGet("configuration")]
    [AllowAnonymous]
    public ActionResult<PushConfigurationResponse> GetConfiguration() =>
        Ok(new PushConfigurationResponse(keyMaterial.PublicKey));

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe(PushSubscriptionRequest request)
    {
        var userId = authorization.GetCurrentUserId();
        var existing = await db.PushSubscriptions
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Endpoint == request.Endpoint);

        if (existing is null)
        {
            db.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.Keys.P256dh,
                Auth = request.Keys.Auth,
                UserAgent = Request.Headers.UserAgent.ToString()
            });
        }
        else
        {
            existing.P256dh = request.Keys.P256dh;
            existing.Auth = request.Keys.Auth;
            existing.UserAgent = Request.Headers.UserAgent.ToString();
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("unsubscribe")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromQuery] string endpoint)
    {
        var userId = authorization.GetCurrentUserId();
        var subscription = await db.PushSubscriptions
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Endpoint == endpoint);
        if (subscription is not null)
        {
            db.PushSubscriptions.Remove(subscription);
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<PushSubscriptionsStatusResponse>> GetStatus()
    {
        var userId = authorization.GetCurrentUserId();
        var items = await subscriptions.Where(item => item.UserId == userId)
            .Select(item => new PushSubscriptionStatusResponse(
                item.Id, item.CreatedAt, item.LastUsedAt, item.UserAgent ?? "Unknown"))
            .ToListAsync();

        return Ok(new PushSubscriptionsStatusResponse(items.Count > 0, items.Count, items));
    }

    [HttpPost("test")]
    [Authorize]
    public async Task<ActionResult<PushNotificationResult>> SendTestNotification()
    {
        var userId = authorization.GetCurrentUserId();
        var result = await pushNotifications.SendToUserAsync(
            userId,
            "Notifications are working",
            "This device is ready to receive reminders from your Habit Space.",
            "/settings#notifications");
        return Ok(result);
    }
}