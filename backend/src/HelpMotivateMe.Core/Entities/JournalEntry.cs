namespace HelpMotivateMe.Core.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly EntryDate { get; set; }

    // Optional linking - only one can be set (enforced by check constraint)
    public Guid? HabitStackId { get; set; }
    public Guid? TaskItemId { get; set; }

    // Author of the entry (null for entries created before this feature, defaults to UserId for owner entries)
    public Guid? AuthorUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public User? Author { get; set; }
    public HabitStack? HabitStack { get; set; }
    public TaskItem? TaskItem { get; set; }
    public ICollection<JournalImage> Images { get; set; } = [];
}
