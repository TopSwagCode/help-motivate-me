using HelpMotivateMe.Core.DTOs.Polar;

namespace HelpMotivateMe.Core.Interfaces;

public interface IPolarService
{
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string productId,
        Guid userId,
        string userEmail,
        string successUrl,
        CancellationToken cancellationToken = default);

    Task<SubscriptionInfo?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    Task CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    bool ValidateWebhookSignature(string payload, string signature);
}
