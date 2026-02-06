using HelpMotivateMe.Core.Interfaces;

namespace HelpMotivateMe.Api.Services;

public class ScheduledPushNotificationService
{
    private readonly ILogger<ScheduledPushNotificationService> _logger;
    private readonly IPushNotificationService _pushNotificationService;

    public ScheduledPushNotificationService(
        IPushNotificationService pushNotificationService,
        ILogger<ScheduledPushNotificationService> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task SendScheduledNotificationAsync()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var title = "Scheduled Reminder";
        var body = $"Your 6-hour check-in! Sent at {timestamp}";
        var url = "/today";

        _logger.LogInformation("Sending scheduled push notification at {Timestamp}", timestamp);

        var result = await _pushNotificationService.SendToAllAsync(title, body, url);

        _logger.LogInformation(
            "Scheduled push notification sent. Total: {Total}, Success: {Success}, Failed: {Failed}",
            result.TotalSubscriptions,
            result.SuccessCount,
            result.FailureCount);
    }
}