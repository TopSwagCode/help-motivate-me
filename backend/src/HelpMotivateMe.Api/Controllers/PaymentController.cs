using System.Security.Claims;
using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Polar;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPolarService _polarService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        AppDbContext db,
        IPolarService polarService,
        IConfiguration configuration,
        ILogger<PaymentController> logger)
    {
        _db = db;
        _polarService = polarService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("checkout")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckout(
        [FromBody] CreateCheckoutRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Map tier + interval to product ID
        var productId = GetProductId(request.Tier, request.BillingInterval);
        if (productId == null)
            return BadRequest(new { message = "Invalid tier or billing interval" });

        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        var successUrl = $"{frontendUrl}/settings?tab=membership&checkout=success";

        var session = await _polarService.CreateCheckoutSessionAsync(
            productId,
            userId.Value,
            user.Email,
            successUrl);

        return Ok(session);
    }

    [HttpGet("subscription")]
    [Authorize]
    public async Task<ActionResult<SubscriptionStatusResponse>> GetSubscriptionStatus()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var subscription = await _db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return Ok(new SubscriptionStatusResponse(
                HasActiveSubscription: false,
                Tier: null,
                BillingInterval: null,
                CurrentPeriodEnd: null,
                CancelAtPeriodEnd: null));
        }

        var tier = GetTierFromProductId(subscription.ProductId);

        return Ok(new SubscriptionStatusResponse(
            HasActiveSubscription: true,
            Tier: tier,
            BillingInterval: subscription.BillingInterval,
            CurrentPeriodEnd: subscription.CurrentPeriodEnd,
            CancelAtPeriodEnd: subscription.CanceledAt != null));
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var subscription = await _db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == "active")
            .FirstOrDefaultAsync();

        if (subscription == null)
            return NotFound(new { message = "No active subscription found" });

        await _polarService.CancelSubscriptionAsync(subscription.PolarSubscriptionId);

        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Polar-Signature"].FirstOrDefault() ?? "";

        if (!_polarService.ValidateWebhookSignature(payload, signature))
        {
            _logger.LogWarning("Invalid webhook signature");
            return Unauthorized();
        }

        var webhook = JsonSerializer.Deserialize<JsonElement>(payload);
        var eventType = webhook.GetProperty("type").GetString();

        _logger.LogInformation("Received Polar webhook: {EventType}", eventType);

        switch (eventType)
        {
            case "subscription.created":
            case "subscription.updated":
                await HandleSubscriptionUpdate(webhook.GetProperty("data"));
                break;
            case "subscription.canceled":
                await HandleSubscriptionCanceled(webhook.GetProperty("data"));
                break;
            case "checkout.completed":
                await HandleCheckoutCompleted(webhook.GetProperty("data"));
                break;
        }

        return Ok();
    }

    private async Task HandleSubscriptionUpdate(JsonElement data)
    {
        var subscriptionId = data.GetProperty("id").GetString()!;
        var status = data.GetProperty("status").GetString()!;
        var productId = data.GetProperty("product_id").GetString()!;
        var customerId = data.GetProperty("customer_id").GetString()!;

        // Get user ID from customer external_id
        string? externalId = null;
        if (data.TryGetProperty("customer", out var customer) &&
            customer.TryGetProperty("external_id", out var extId) &&
            extId.ValueKind != JsonValueKind.Null)
        {
            externalId = extId.GetString();
        }

        if (!Guid.TryParse(externalId, out var userId))
        {
            _logger.LogWarning("Could not determine user ID for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.PolarSubscriptionId == subscriptionId);

        var billingInterval = GetBillingIntervalFromProductId(productId);

        if (subscription == null)
        {
            subscription = new Subscription
            {
                UserId = userId,
                PolarSubscriptionId = subscriptionId,
                PolarCustomerId = customerId,
                ProductId = productId,
                Status = status,
                BillingInterval = billingInterval
            };
            _db.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Status = status;
            subscription.ProductId = productId;
            subscription.BillingInterval = billingInterval;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        if (data.TryGetProperty("current_period_start", out var periodStart) && periodStart.ValueKind != JsonValueKind.Null)
            subscription.CurrentPeriodStart = periodStart.GetDateTime();
        if (data.TryGetProperty("current_period_end", out var periodEnd) && periodEnd.ValueKind != JsonValueKind.Null)
            subscription.CurrentPeriodEnd = periodEnd.GetDateTime();

        // Update user membership tier
        if (status == "active")
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.MembershipTier = GetMembershipTierFromProductId(productId);
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task HandleSubscriptionCanceled(JsonElement data)
    {
        var subscriptionId = data.GetProperty("id").GetString()!;

        var subscription = await _db.Subscriptions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.PolarSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = "canceled";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Downgrade user to Free tier
            subscription.User.MembershipTier = MembershipTier.Free;
            subscription.User.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }

    private Task HandleCheckoutCompleted(JsonElement data)
    {
        // Checkout completed - subscription will be created via subscription.created event
        _logger.LogInformation("Checkout completed: {CheckoutId}",
            data.GetProperty("id").GetString());
        return Task.CompletedTask;
    }

    private string? GetProductId(string tier, string billingInterval)
    {
        var key = $"Polar:Products:{tier}{char.ToUpper(billingInterval[0])}{billingInterval[1..]}";
        return _configuration[key];
    }

    private string GetTierFromProductId(string productId)
    {
        var products = _configuration.GetSection("Polar:Products");
        foreach (var product in products.GetChildren())
        {
            if (product.Value == productId)
            {
                return product.Key.Contains("Plus") ? "Plus" : "Pro";
            }
        }
        return "Free";
    }

    private MembershipTier GetMembershipTierFromProductId(string productId)
    {
        var tier = GetTierFromProductId(productId);
        return tier switch
        {
            "Plus" => MembershipTier.Plus,
            "Pro" => MembershipTier.Pro,
            _ => MembershipTier.Free
        };
    }

    private string GetBillingIntervalFromProductId(string productId)
    {
        var products = _configuration.GetSection("Polar:Products");
        foreach (var product in products.GetChildren())
        {
            if (product.Value == productId)
            {
                return product.Key.Contains("Monthly") ? "monthly" : "yearly";
            }
        }
        return "monthly";
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
