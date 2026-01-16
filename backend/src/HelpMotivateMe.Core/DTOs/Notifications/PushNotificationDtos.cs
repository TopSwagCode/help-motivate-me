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
    string Username,
    string Email,
    bool HasPushEnabled,
    int SubscriptionCount,
    DateTime? LastPushSentAt
);
