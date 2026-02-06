namespace HelpMotivateMe.Core.Entities;

public class BuddyInviteToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public Guid InviterUserId { get; set; }
    public required string InvitedEmail { get; set; }
    public Guid BuddyUserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User InviterUser { get; set; } = null!;
    public User BuddyUser { get; set; } = null!;
}