using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public bool IsRepeatable { get; set; }
    public Guid? RepeatScheduleId { get; set; }
    public int SortOrder { get; set; }

    // Identity-based tracking (Atomic Habits)
    public Guid? IdentityId { get; set; }

    // 2-Minute Rule (Atomic Habits)
    public int? EstimatedMinutes { get; set; }
    public bool IsTinyHabit { get; set; } = false;
    public Guid? FullVersionTaskId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Goal Goal { get; set; } = null!;
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> Subtasks { get; set; } = [];
    public RepeatSchedule? RepeatSchedule { get; set; }
    public Identity? Identity { get; set; }
    public TaskItem? FullVersionTask { get; set; }
    public ICollection<TaskItem> TinyVersions { get; set; } = [];
}
