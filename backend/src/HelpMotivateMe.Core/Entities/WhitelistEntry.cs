namespace HelpMotivateMe.Core.Entities;

public class WhitelistEntry
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Guid? AddedByUserId { get; set; }
    public DateTime? InvitedAt { get; set; }

    // Navigation properties
    public User? AddedByUser { get; set; }
}