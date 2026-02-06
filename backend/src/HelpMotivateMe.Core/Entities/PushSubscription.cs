namespace HelpMotivateMe.Core.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// The push service endpoint URL
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The P256DH key for encryption
    /// </summary>
    public required string P256dh { get; set; }

    /// <summary>
    /// The auth secret for encryption
    /// </summary>
    public required string Auth { get; set; }

    /// <summary>
    /// User agent string for debugging
    /// </summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
