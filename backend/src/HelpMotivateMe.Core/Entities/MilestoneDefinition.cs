namespace HelpMotivateMe.Core.Entities;

/// <summary>
///     Milestone rules stored as data, not code. Defines the conditions for awarding milestones.
/// </summary>
public class MilestoneDefinition
{
    public Guid Id { get; set; }

    /// <summary>
    ///     Unique stable identifier for the milestone (e.g., "first_login", "habits_100").
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    ///     i18n key for the milestone title (e.g., "milestones.first_login.title").
    /// </summary>
    public required string TitleKey { get; set; }

    /// <summary>
    ///     i18n key for the milestone description (e.g., "milestones.first_login.description").
    /// </summary>
    public required string DescriptionKey { get; set; }

    /// <summary>
    ///     Icon identifier (e.g., emoji or icon name).
    /// </summary>
    public required string Icon { get; set; }

    /// <summary>
    ///     The event type that triggers evaluation (e.g., "UserLoggedIn", "HabitCompleted").
    /// </summary>
    public required string TriggerEvent { get; set; }

    /// <summary>
    ///     Rule type: "count", "window_count", "return_after_gap".
    /// </summary>
    public required string RuleType { get; set; }

    /// <summary>
    ///     JSON data for rule evaluation (e.g., {"field": "login_count", "threshold": 7}).
    /// </summary>
    public required string RuleData { get; set; }

    /// <summary>
    ///     Animation type for celebration (default: "confetti").
    /// </summary>
    public string AnimationType { get; set; } = "confetti";

    /// <summary>
    ///     Optional JSON data for animation customization.
    /// </summary>
    public string? AnimationData { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserMilestone> UserMilestones { get; set; } = [];
}