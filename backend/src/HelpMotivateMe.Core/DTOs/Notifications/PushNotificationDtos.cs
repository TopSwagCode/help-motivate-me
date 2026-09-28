namespace HelpMotivateMe.Core.DTOs.Notifications;

public sealed record PushConfigurationResponse(string PublicKey);
public sealed record PushSubscriptionRequest(string Endpoint, PushSubscriptionKeys Keys);
public sealed record PushSubscriptionKeys(string P256dh, string Auth);
public sealed record PushSubscriptionStatusResponse(Guid Id, DateTime CreatedAt, DateTime? LastUsedAt, string UserAgent);
public sealed record PushSubscriptionsStatusResponse(
    bool HasSubscriptions,
    int SubscriptionCount,
    IEnumerable<PushSubscriptionStatusResponse> Subscriptions);
public sealed record PushNotificationResult(int TotalSubscriptions, int SuccessCount, int FailureCount);