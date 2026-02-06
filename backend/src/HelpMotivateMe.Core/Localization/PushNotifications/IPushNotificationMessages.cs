namespace HelpMotivateMe.Core.Localization.PushNotifications;

/// <summary>
/// Interface for localized push notification messages.
/// </summary>
public interface IPushNotificationMessages
{
    // Daily Identity Commitment - Morning (Identity invitation)
    string MorningCommitmentTitle { get; }
    string MorningCommitmentBody { get; }

    // Daily Identity Commitment - Afternoon (Gentle reminder)
    string AfternoonReminderTitle { get; }
    string AfternoonReminderBody { get; }

    // Daily Identity Commitment - Evening (Reflection prompt)
    string EveningReflectionTitle { get; }
    string EveningReflectionBody { get; }
}
