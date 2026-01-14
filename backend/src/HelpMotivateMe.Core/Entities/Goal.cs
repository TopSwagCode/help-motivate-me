namespace HelpMotivateMe.Core.Entities;

public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Optional identity link - tasks created under this goal inherit this identity by default
    public Guid? IdentityId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Identity? Identity { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
