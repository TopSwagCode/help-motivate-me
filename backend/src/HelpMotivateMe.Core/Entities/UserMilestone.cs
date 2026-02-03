namespace HelpMotivateMe.Core.Entities;

/// <summary>
/// Tracks awarded milestones for users. Once awarded, never revoked.
/// </summary>
public class UserMilestone
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MilestoneDefinitionId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public bool HasBeenSeen { get; set; } = false;

    // Navigation properties
    public User User { get; set; } = null!;
    public MilestoneDefinition MilestoneDefinition { get; set; } = null!;
}
