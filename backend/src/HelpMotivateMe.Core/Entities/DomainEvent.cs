namespace HelpMotivateMe.Core.Entities;

/// <summary>
///     Append-only event log for tracking user actions that can trigger milestones.
/// </summary>
public class DomainEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string EventType { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}