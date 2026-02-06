using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;

namespace HelpMotivateMe.Core.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    ///     Send a push notification to a specific user (all their subscriptions)
    /// </summary>
    Task<PushNotificationResult> SendToUserAsync(Guid userId, string title, string body, string? url = null);

    /// <summary>
    ///     Send a push notification to multiple users
    /// </summary>
    Task<PushNotificationResult> SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body,
        string? url = null);

    /// <summary>
    ///     Send a push notification to all subscribed users
    /// </summary>
    Task<PushNotificationResult> SendToAllAsync(string title, string body, string? url = null);

    /// <summary>
    ///     Send a push notification to a specific subscription
    /// </summary>
    Task<bool> SendToSubscriptionAsync(PushSubscription subscription, string title, string body, string? url = null);
}