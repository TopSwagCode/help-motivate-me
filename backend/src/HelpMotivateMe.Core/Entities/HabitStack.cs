namespace HelpMotivateMe.Core.Entities;

public class HabitStack
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Link to identity (habit stacks reinforce identity)
    public Guid? IdentityId { get; set; }

    // Trigger for the stack (e.g., "After I wake up")
    public string? TriggerCue { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Identity? Identity { get; set; }
    public ICollection<HabitStackItem> Items { get; set; } = [];
}
