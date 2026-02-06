namespace HelpMotivateMe.Core.Entities;

public class JournalReaction
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid UserId { get; set; }
    public required string Emoji { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public JournalEntry JournalEntry { get; set; } = null!;
    public User User { get; set; } = null!;
}