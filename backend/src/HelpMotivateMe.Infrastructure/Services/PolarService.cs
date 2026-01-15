using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Polar;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpMotivateMe.Infrastructure.Services;

public class PolarService : IPolarService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PolarService> _logger;

    private const string BaseUrl = "https://api.polar.sh";

    public PolarService(HttpClient httpClient, IConfiguration configuration, ILogger<PolarService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["Polar:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string productId,
        Guid userId,
        string userEmail,
        string successUrl,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            products = new[] { productId },
            success_url = successUrl,
            customer_email = userEmail,
            customer_external_id = userId.ToString(),
            metadata = new { user_id = userId.ToString() }
        };

        _logger.LogInformation("Creating Polar checkout session for user {UserId} with product {ProductId}", userId, productId);

        var response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}/v1/checkouts/",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create checkout session: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Failed to create checkout session: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new CheckoutSessionResponse(
            result.GetProperty("url").GetString()!,
            result.GetProperty("id").GetString()!
        );
    }

    public async Task<SubscriptionInfo?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{BaseUrl}/v1/subscriptions/{subscriptionId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get subscription {SubscriptionId}: {StatusCode}", subscriptionId, response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new SubscriptionInfo(
            result.GetProperty("id").GetString()!,
            result.GetProperty("status").GetString()!,
            result.GetProperty("product_id").GetString()!,
            result.TryGetProperty("current_period_start", out var start) && start.ValueKind != JsonValueKind.Null
                ? start.GetDateTime() : null,
            result.TryGetProperty("current_period_end", out var end) && end.ValueKind != JsonValueKind.Null
                ? end.GetDateTime() : null
        );
    }

    public async Task CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Canceling subscription {SubscriptionId}", subscriptionId);

        var response = await _httpClient.DeleteAsync(
            $"{BaseUrl}/v1/subscriptions/{subscriptionId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to cancel subscription: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Failed to cancel subscription: {response.StatusCode}");
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        var secret = _configuration["Polar:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Webhook secret not configured");
            return false;
        }

        // Polar uses HMAC-SHA256 for webhook signatures
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(computedSignature));
    }
}
