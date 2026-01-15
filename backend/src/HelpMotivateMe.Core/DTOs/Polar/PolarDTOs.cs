namespace HelpMotivateMe.Core.DTOs.Polar;

// Request to create checkout
public record CreateCheckoutRequest(
    string Tier,           // "Plus" or "Pro"
    string BillingInterval // "monthly" or "yearly"
);

// Response from checkout creation
public record CheckoutSessionResponse(
    string CheckoutUrl,
    string SessionId
);

// Response for subscription info
public record SubscriptionInfo(
    string Id,
    string Status,
    string ProductId,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd
);

// API response for frontend
public record SubscriptionStatusResponse(
    bool HasActiveSubscription,
    string? Tier,
    string? BillingInterval,
    DateTime? CurrentPeriodEnd,
    bool? CancelAtPeriodEnd
);
