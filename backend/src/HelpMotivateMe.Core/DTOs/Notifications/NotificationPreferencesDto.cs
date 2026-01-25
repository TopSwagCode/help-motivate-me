namespace HelpMotivateMe.Core.DTOs.Notifications;

public record NotificationPreferencesResponse(
    bool NotificationsEnabled,
    bool EmailEnabled,
    bool SmsEnabled,
    bool PhoneEnabled,
    bool HabitRemindersEnabled,
    bool GoalRemindersEnabled,
    bool DailyDigestEnabled,
    bool StreakAlertsEnabled,
    bool MotivationalQuotesEnabled,
    bool WeeklyReviewEnabled,
    bool BuddyUpdatesEnabled,
    bool DailyCommitmentEnabled,
    string CommitmentDefaultMode,
    int SelectedDays,
    string PreferredTimeSlot,
    string? CustomTimeStart,
    string? CustomTimeEnd,
    string TimezoneId,
    int UtcOffsetMinutes
);

public record UpdateNotificationPreferencesRequest(
    bool? NotificationsEnabled = null,
    bool? EmailEnabled = null,
    bool? SmsEnabled = null,
    bool? HabitRemindersEnabled = null,
    bool? GoalRemindersEnabled = null,
    bool? DailyDigestEnabled = null,
    bool? StreakAlertsEnabled = null,
    bool? MotivationalQuotesEnabled = null,
    bool? WeeklyReviewEnabled = null,
    bool? BuddyUpdatesEnabled = null,
    bool? DailyCommitmentEnabled = null,
    string? CommitmentDefaultMode = null,
    int? SelectedDays = null,
    string? PreferredTimeSlot = null,
    string? CustomTimeStart = null,
    string? CustomTimeEnd = null,
    string? TimezoneId = null,
    int? UtcOffsetMinutes = null
);
