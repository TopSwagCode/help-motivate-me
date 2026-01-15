namespace HelpMotivateMe.Core.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string PolarSubscriptionId { get; set; }
    public required string PolarCustomerId { get; set; }
    public required string ProductId { get; set; }
    public required string Status { get; set; } // active, canceled, past_due
    public required string BillingInterval { get; set; } // monthly, yearly
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
