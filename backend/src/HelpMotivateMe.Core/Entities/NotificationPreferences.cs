using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class NotificationPreferences
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Master toggle
    public bool NotificationsEnabled { get; set; } = true;

    // Delivery channels
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PhoneEnabled { get; set; } = false;

    // Notification types
    public bool HabitRemindersEnabled { get; set; } = true;
    public bool GoalRemindersEnabled { get; set; } = true;
    public bool DailyDigestEnabled { get; set; } = true;
    public bool StreakAlertsEnabled { get; set; } = true;
    public bool MotivationalQuotesEnabled { get; set; } = true;
    public bool WeeklyReviewEnabled { get; set; } = true;
    public bool BuddyUpdatesEnabled { get; set; } = true;

    // Daily Identity Commitment
    public bool DailyCommitmentEnabled { get; set; } = true;
    public string CommitmentDefaultMode { get; set; } = "weakest"; // "weakest" or "strongest"

    // Schedule - Days
    public NotificationDays SelectedDays { get; set; } = NotificationDays.All;

    // Schedule - Time slots
    public TimeSlot PreferredTimeSlot { get; set; } = TimeSlot.Morning;
    public TimeOnly? CustomTimeStart { get; set; }
    public TimeOnly? CustomTimeEnd { get; set; }

    // Timezone
    public string TimezoneId { get; set; } = "UTC";
    public int UtcOffsetMinutes { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
