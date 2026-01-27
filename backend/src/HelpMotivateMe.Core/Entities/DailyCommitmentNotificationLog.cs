using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

/// <summary>
/// Tracks sent daily commitment notifications to prevent duplicate sends.
/// Each user can receive at most one notification per time slot per day.
/// </summary>
public class DailyCommitmentNotificationLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The local date in the user's timezone when the notification was sent.
    /// </summary>
    public DateOnly LocalDate { get; set; }
    
    /// <summary>
    /// The notification window (Morning, Afternoon, Evening) for this notification.
    /// </summary>
    public TimeSlot TimeSlot { get; set; }
    
    /// <summary>
    /// UTC timestamp when the notification was actually sent.
    /// </summary>
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}
