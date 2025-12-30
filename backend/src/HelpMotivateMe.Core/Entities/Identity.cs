namespace HelpMotivateMe.Core.Entities;

public class Identity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<HabitStack> HabitStacks { get; set; } = [];
}
