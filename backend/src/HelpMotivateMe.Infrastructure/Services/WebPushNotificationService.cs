using System.Net;
using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebPush;
using PushSubscriptionEntity = HelpMotivateMe.Core.Entities.PushSubscription;

namespace HelpMotivateMe.Infrastructure.Services;

public sealed class WebPushNotificationService(
    AppDbContext db,
    VapidKeyMaterial keyMaterial,
    ILogger<WebPushNotificationService> logger) : IPushNotificationService
{
    private readonly WebPushClient _client = new();
    private readonly VapidDetails _vapidDetails = new(keyMaterial.Subject, keyMaterial.PublicKey, keyMaterial.PrivateKey);

    public async Task<PushNotificationResult> SendToUserAsync(Guid userId, string title, string body,
        string? url = null)
    {
        var subscriptions = await db.PushSubscriptions.Where(item => item.UserId == userId).ToListAsync();
        var successCount = 0;

        foreach (var subscription in subscriptions)
            if (await SendToSubscriptionAsync(subscription, title, body, url)) successCount++;

        return new PushNotificationResult(subscriptions.Count, successCount, subscriptions.Count - successCount);
    }

    public async Task<bool> SendToSubscriptionAsync(PushSubscriptionEntity subscription, string title, string body,
        string? url = null)
    {
        try
        {
            var browserSubscription = new WebPush.PushSubscription(
                subscription.Endpoint, subscription.P256dh, subscription.Auth);
            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                url = url ?? "/today",
                icon = "/android-chrome-192x192.png",
                badge = "/android-chrome-192x192.png"
            });

            await _client.SendNotificationAsync(browserSubscription, payload, _vapidDetails);
            subscription.LastUsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return true;
        }
        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            logger.LogInformation("Removing expired push subscription {SubscriptionId}", subscription.Id);
            db.PushSubscriptions.Remove(subscription);
            await db.SaveChangesAsync();
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send push notification to subscription {SubscriptionId}",
                subscription.Id);
            return false;
        }
    }
}