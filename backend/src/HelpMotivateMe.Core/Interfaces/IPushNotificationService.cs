using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;

namespace HelpMotivateMe.Core.Interfaces;

public interface IPushNotificationService
{
    Task<PushNotificationResult> SendToUserAsync(Guid userId, string title, string body, string? url = null);
    Task<bool> SendToSubscriptionAsync(PushSubscription subscription, string title, string body, string? url = null);
}