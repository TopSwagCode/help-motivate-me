namespace HelpMotivateMe.Core.Localization.PushNotifications;

public class EnglishPushNotificationMessages : IPushNotificationMessages
{
    // Daily Identity Commitment - Morning (Identity invitation)
    public string MorningCommitmentTitle => "Who are you choosing to be today?";
    public string MorningCommitmentBody => "Take a moment to commit to one small action that proves your identity.";
    
    // Daily Identity Commitment - Afternoon (Gentle reminder)
    public string AfternoonReminderTitle => "One small action proves who you are";
    public string AfternoonReminderBody => "You haven't completed your daily commitment yet. Still time to show up as who you want to be.";
    
    // Daily Identity Commitment - Evening (Reflection prompt)
    public string EveningReflectionTitle => "Did you show up as who you wanted to be?";
    public string EveningReflectionBody => "Take a moment to reflect on your day and celebrate any progress, no matter how small.";
}
