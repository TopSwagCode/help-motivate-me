using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class DailyIdentityCommitment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly CommitmentDate { get; set; }
    public Guid IdentityId { get; set; }
    public required string ActionDescription { get; set; }

    // Optional link to existing habit/task
    public Guid? LinkedHabitStackItemId { get; set; }
    public Guid? LinkedTaskId { get; set; }

    // Status tracking
    public DailyCommitmentStatus Status { get; set; } = DailyCommitmentStatus.Committed;
    public DateTime? CompletedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Identity Identity { get; set; } = null!;
    public HabitStackItem? LinkedHabitStackItem { get; set; }
    public TaskItem? LinkedTask { get; set; }
}