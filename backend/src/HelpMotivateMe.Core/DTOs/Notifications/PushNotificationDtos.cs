namespace HelpMotivateMe.Core.DTOs.Notifications;

public record PushSubscriptionRequest(
    string Endpoint,
    PushSubscriptionKeys Keys
);

public record PushSubscriptionKeys(
    string P256dh,
    string Auth
);

public record SendPushNotificationRequest(
    string Title,
    string Body,
    string? Url = null,
    string? Icon = null
);

public record SendPushToUserRequest(
    Guid UserId,
    string Title,
    string Body,
    string? Url = null
);

public record SendPushToAllRequest(
    string Title,
    string Body,
    string? Url = null
);

public record PushNotificationResult(
    int TotalSubscriptions,
    int SuccessCount,
    int FailureCount,
    List<string> Errors
);

public record UserPushStatus(
    Guid UserId,
    string Email,
    bool HasPushEnabled,
    int SubscriptionCount,
    DateTime? LastPushSentAt
);

/// <summary>
/// Response DTO for user's push subscription status - excludes sensitive endpoint/key data
/// </summary>
public record PushSubscriptionStatusResponse(
    Guid Id,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    string UserAgent
);

/// <summary>
/// Response DTO for push subscription list status
/// </summary>
public record PushSubscriptionsStatusResponse(
    bool HasSubscriptions,
    int SubscriptionCount,
    IEnumerable<PushSubscriptionStatusResponse> Subscriptions
);

/// <summary>
/// Response DTO for push notification statistics
/// </summary>
public record PushNotificationStatsResponse(
    int TotalSubscriptions,
    int UsersWithPush,
    DateTime? OldestSubscription,
    DateTime? NewestSubscription
);
