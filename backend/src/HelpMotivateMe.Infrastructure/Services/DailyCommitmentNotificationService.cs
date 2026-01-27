using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization.PushNotifications;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpMotivateMe.Infrastructure.Services;

/// <summary>
/// Service for processing daily identity commitment push notifications.
/// Scans eligible users and sends at most one notification per time slot per day.
/// </summary>
public class DailyCommitmentNotificationService : IDailyCommitmentNotificationService
{
    private readonly AppDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<DailyCommitmentNotificationService> _logger;

    // Time windows for each slot (from existing TimeSlot enum comments)
    private static readonly Dictionary<TimeSlot, (TimeOnly Start, TimeOnly End)> TimeWindows = new()
    {
        { TimeSlot.Morning, (new TimeOnly(7, 0), new TimeOnly(9, 0)) },      // 07:00 - 09:00
        { TimeSlot.Afternoon, (new TimeOnly(12, 0), new TimeOnly(14, 0)) },  // 12:00 - 14:00
        { TimeSlot.Evening, (new TimeOnly(18, 0), new TimeOnly(20, 0)) }     // 18:00 - 20:00
    };

    public DailyCommitmentNotificationService(
        AppDbContext context,
        IPushNotificationService pushNotificationService,
        ILogger<DailyCommitmentNotificationService> logger)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ProcessEligibleUsersAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var notificationsSent = 0;

        _logger.LogInformation("Starting daily commitment notification processing at {UtcNow}", utcNow);

        // Fetch candidate users with required data in a single query
        var candidates = await GetCandidateUsersAsync(utcNow, cancellationToken);

        _logger.LogInformation("Found {CandidateCount} candidate users for notification", candidates.Count);

        foreach (var candidate in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var sent = await ProcessUserAsync(candidate, utcNow, cancellationToken);
                if (sent)
                    notificationsSent++;
            }
            catch (Exception ex)
            {
                // Log and continue - single user failure must not fail the worker
                _logger.LogError(ex, "Error processing notification for user {UserId}", candidate.UserId);
            }
        }

        _logger.LogInformation("Completed daily commitment notification processing. Sent {NotificationCount} notifications", notificationsSent);
        return notificationsSent;
    }

    /// <summary>
    /// Fetches users who are candidates for notification (pre-filtered at database level).
    /// </summary>
    private async Task<List<NotificationCandidate>> GetCandidateUsersAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(utcNow);

        // Query users where:
        // - Notifications are enabled
        // - Daily commitment notifications are enabled
        // - No completed or dismissed commitment for today
        var candidates = await _context.NotificationPreferences
            .AsNoTracking()
            .Include(np => np.User)
            .Where(np => np.NotificationsEnabled)
            .Where(np => np.DailyCommitmentEnabled)
            .Where(np => np.User.IsActive)
            .Where(np => !_context.Set<DailyIdentityCommitment>()
                .Any(dc => dc.UserId == np.UserId &&
                          dc.CommitmentDate == today &&
                          (dc.Status == DailyCommitmentStatus.Completed || dc.Status == DailyCommitmentStatus.Dismissed)))
            .Select(np => new NotificationCandidate
            {
                UserId = np.UserId,
                PreferredLanguage = np.User.PreferredLanguage,
                TimezoneId = np.TimezoneId,
                UtcOffsetMinutes = np.UtcOffsetMinutes,
                PreferredTimeSlot = np.PreferredTimeSlot,
                CustomTimeStart = np.CustomTimeStart,
                CustomTimeEnd = np.CustomTimeEnd,
                SelectedDays = np.SelectedDays
            })
            .ToListAsync(cancellationToken);

        return candidates;
    }

    /// <summary>
    /// Processes a single user and sends notification if eligible.
    /// </summary>
    private async Task<bool> ProcessUserAsync(NotificationCandidate candidate, DateTime utcNow, CancellationToken cancellationToken)
    {
        // Resolve user's local time
        var localDateTime = ResolveLocalTime(utcNow, candidate.TimezoneId, candidate.UtcOffsetMinutes);
        var localDate = DateOnly.FromDateTime(localDateTime);
        var localTime = TimeOnly.FromDateTime(localDateTime);

        // Check if today is a selected notification day
        if (!IsDaySelected(localDateTime.DayOfWeek, candidate.SelectedDays))
        {
            return false;
        }

        // Resolve which time slot we're currently in (if any)
        var currentSlot = ResolveCurrentTimeSlot(localTime, candidate.PreferredTimeSlot, candidate.CustomTimeStart, candidate.CustomTimeEnd);
        if (currentSlot == null)
        {
            return false;
        }

        // Check if we've already sent a notification for this slot today
        var alreadySent = await _context.Set<DailyCommitmentNotificationLog>()
            .AnyAsync(log => log.UserId == candidate.UserId &&
                            log.LocalDate == localDate &&
                            log.TimeSlot == currentSlot.Value, cancellationToken);

        if (alreadySent)
        {
            return false;
        }

        // Get localized messages
        var messages = PushNotificationMessageProvider.GetMessages(candidate.PreferredLanguage);
        var (title, body) = GetNotificationContent(currentSlot.Value, messages);

        // Send the notification
        var result = await _pushNotificationService.SendToUserAsync(candidate.UserId, title, body, "/today");

        if (result.SuccessCount > 0)
        {
            // Persist the notification log
            var log = new DailyCommitmentNotificationLog
            {
                Id = Guid.NewGuid(),
                UserId = candidate.UserId,
                LocalDate = localDate,
                TimeSlot = currentSlot.Value,
                SentAtUtc = utcNow
            };

            _context.Set<DailyCommitmentNotificationLog>().Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Sent {TimeSlot} notification to user {UserId}", currentSlot.Value, candidate.UserId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the user's local time from UTC using their timezone settings.
    /// </summary>
    private static DateTime ResolveLocalTime(DateTime utcNow, string timezoneId, int utcOffsetMinutes)
    {
        // Try to use TimezoneId first (authoritative)
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
        catch (InvalidTimeZoneException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
    }

    /// <summary>
    /// Determines if the current day of week is selected for notifications.
    /// </summary>
    private static bool IsDaySelected(DayOfWeek dayOfWeek, NotificationDays selectedDays)
    {
        var dayFlag = dayOfWeek switch
        {
            DayOfWeek.Monday => NotificationDays.Monday,
            DayOfWeek.Tuesday => NotificationDays.Tuesday,
            DayOfWeek.Wednesday => NotificationDays.Wednesday,
            DayOfWeek.Thursday => NotificationDays.Thursday,
            DayOfWeek.Friday => NotificationDays.Friday,
            DayOfWeek.Saturday => NotificationDays.Saturday,
            DayOfWeek.Sunday => NotificationDays.Sunday,
            _ => NotificationDays.None
        };

        return selectedDays.HasFlag(dayFlag);
    }

    /// <summary>
    /// Resolves which time slot (if any) the current local time falls into.
    /// Supports both same-day and overnight windows for custom times.
    /// </summary>
    private static TimeSlot? ResolveCurrentTimeSlot(TimeOnly localTime, TimeSlot preferredSlot, TimeOnly? customStart, TimeOnly? customEnd)
    {
        // If custom times are set, use them regardless of PreferredTimeSlot
        if (customStart.HasValue && customEnd.HasValue)
        {
            if (IsTimeInWindow(localTime, customStart.Value, customEnd.Value))
            {
                // Return the preferred slot as the "type" for messaging purposes
                // but the eligibility is based on custom window
                return preferredSlot != TimeSlot.Custom ? preferredSlot : TimeSlot.Morning;
            }
            return null;
        }

        // Check standard time slots based on preference
        // Only check the user's preferred slot
        if (preferredSlot == TimeSlot.Custom)
        {
            // Custom selected but no custom times set - use Morning as default
            if (TimeWindows.TryGetValue(TimeSlot.Morning, out var morningWindow) &&
                IsTimeInWindow(localTime, morningWindow.Start, morningWindow.End))
            {
                return TimeSlot.Morning;
            }
            return null;
        }

        // For standard slots, we check all three windows during the day
        // to enable the "invitation → reminder → reflection" flow
        foreach (var (slot, window) in TimeWindows)
        {
            if (IsTimeInWindow(localTime, window.Start, window.End))
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a time falls within a window, supporting overnight windows.
    /// </summary>
    private static bool IsTimeInWindow(TimeOnly time, TimeOnly start, TimeOnly end)
    {
        if (start < end)
        {
            // Normal same-day window: [start, end)
            return time >= start && time < end;
        }
        else
        {
            // Overnight window: time >= start OR time < end
            return time >= start || time < end;
        }
    }

    /// <summary>
    /// Gets the notification title and body based on time slot.
    /// </summary>
    private static (string Title, string Body) GetNotificationContent(TimeSlot slot, IPushNotificationMessages messages)
    {
        return slot switch
        {
            TimeSlot.Morning => (messages.MorningCommitmentTitle, messages.MorningCommitmentBody),
            TimeSlot.Afternoon => (messages.AfternoonReminderTitle, messages.AfternoonReminderBody),
            TimeSlot.Evening => (messages.EveningReflectionTitle, messages.EveningReflectionBody),
            _ => (messages.MorningCommitmentTitle, messages.MorningCommitmentBody) // Fallback
        };
    }

    /// <summary>
    /// Internal class to hold candidate user data for processing.
    /// </summary>
    private class NotificationCandidate
    {
        public Guid UserId { get; set; }
        public Language PreferredLanguage { get; set; }
        public string TimezoneId { get; set; } = "UTC";
        public int UtcOffsetMinutes { get; set; }
        public TimeSlot PreferredTimeSlot { get; set; }
        public TimeOnly? CustomTimeStart { get; set; }
        public TimeOnly? CustomTimeEnd { get; set; }
        public NotificationDays SelectedDays { get; set; }
    }
}
