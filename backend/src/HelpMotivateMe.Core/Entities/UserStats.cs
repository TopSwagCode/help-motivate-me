namespace HelpMotivateMe.Core.Entities;

/// <summary>
/// Fast aggregates for milestone evaluation (one row per user).
/// </summary>
public class UserStats
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Counts
    public int LoginCount { get; set; }
    public int TotalWins { get; set; }
    public int TotalHabitsCompleted { get; set; }
    public int TotalTasksCompleted { get; set; }
    public int TotalIdentityProofs { get; set; }
    public int TotalJournalEntries { get; set; }

    // Login tracking for return-after-gap calculation
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PreviousLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}
