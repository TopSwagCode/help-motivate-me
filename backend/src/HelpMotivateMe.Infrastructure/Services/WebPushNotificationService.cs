using System.Net;
using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;
using PushSubscriptionEntity = HelpMotivateMe.Core.Entities.PushSubscription;

namespace HelpMotivateMe.Infrastructure.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly ILogger<WebPushNotificationService> _logger;
    private readonly VapidDetails _vapidDetails;
    private readonly WebPushClient _webPushClient;

    public WebPushNotificationService(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _webPushClient = new WebPushClient();

        var vapidSubject = _configuration["Vapid:Subject"] ?? "mailto:admin@helpmotivateme.app";
        var vapidPublicKey = _configuration["Vapid:PublicKey"]
                             ?? throw new InvalidOperationException("Vapid:PublicKey is not configured");
        var vapidPrivateKey = _configuration["Vapid:PrivateKey"]
                              ?? throw new InvalidOperationException("Vapid:PrivateKey is not configured");

        _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
    }

    public async Task<PushNotificationResult> SendToUserAsync(Guid userId, string title, string body,
        string? url = null)
    {
        var subscriptions = await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return await SendToSubscriptionsAsync(subscriptions, title, body, url);
    }

    public async Task<PushNotificationResult> SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body,
        string? url = null)
    {
        var userIdList = userIds.ToList();
        var subscriptions = await _db.PushSubscriptions
            .Where(s => userIdList.Contains(s.UserId))
            .ToListAsync();

        return await SendToSubscriptionsAsync(subscriptions, title, body, url);
    }

    public async Task<PushNotificationResult> SendToAllAsync(string title, string body, string? url = null)
    {
        var subscriptions = await _db.PushSubscriptions.ToListAsync();
        return await SendToSubscriptionsAsync(subscriptions, title, body, url);
    }

    public async Task<bool> SendToSubscriptionAsync(PushSubscriptionEntity subscription, string title, string body,
        string? url = null)
    {
        try
        {
            var payload = CreatePayload(title, body, url);
            var pushSubscription = new PushSubscription(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth);

            await _webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);

            // Update last used timestamp
            subscription.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
        catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone ||
                                          ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Subscription is no longer valid, remove it
            _logger.LogInformation("Removing expired push subscription {SubscriptionId}", subscription.Id);
            _db.PushSubscriptions.Remove(subscription);
            await _db.SaveChangesAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to subscription {SubscriptionId}", subscription.Id);
            return false;
        }
    }

    private async Task<PushNotificationResult> SendToSubscriptionsAsync(
        List<PushSubscriptionEntity> subscriptions,
        string title,
        string body,
        string? url)
    {
        var errors = new List<string>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var subscription in subscriptions)
        {
            var success = await SendToSubscriptionAsync(subscription, title, body, url);
            if (success)
                successCount++;
            else
                failureCount++;
        }

        return new PushNotificationResult(
            subscriptions.Count,
            successCount,
            failureCount,
            errors
        );
    }

    private static string CreatePayload(string title, string body, string? url)
    {
        var payload = new
        {
            title,
            body,
            url = url ?? "/",
            icon = "/pwa-192x192.png",
            badge = "/pwa-192x192.png"
        };

        return JsonSerializer.Serialize(payload);
    }
}