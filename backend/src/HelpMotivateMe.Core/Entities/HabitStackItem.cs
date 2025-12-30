namespace HelpMotivateMe.Core.Entities;

public class HabitStackItem
{
    public Guid Id { get; set; }
    public Guid HabitStackId { get; set; }

    // Descriptions for the habit chain
    public required string CueDescription { get; set; }     // "After I..."
    public required string HabitDescription { get; set; }   // "I will..."

    public int SortOrder { get; set; }

    // Streak tracking
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastCompletedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public HabitStack HabitStack { get; set; } = null!;
    public ICollection<HabitStackItemCompletion> Completions { get; set; } = [];
}
