namespace HelpMotivateMe.Core.Entities;

public class AccountabilityBuddy
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }           // Main user who added the buddy
    public Guid BuddyUserId { get; set; }      // The accountability buddy
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public User BuddyUser { get; set; } = null!;
}
