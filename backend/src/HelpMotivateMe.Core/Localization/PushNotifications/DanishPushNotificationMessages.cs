namespace HelpMotivateMe.Core.Localization.PushNotifications;

public class DanishPushNotificationMessages : IPushNotificationMessages
{
    // Daily Identity Commitment - Morning (Identity invitation)
    public string MorningCommitmentTitle => "Hvem vælger du at være i dag?";
    public string MorningCommitmentBody => "Tag et øjeblik og forpligt dig til én lille handling, der beviser din identitet.";
    
    // Daily Identity Commitment - Afternoon (Gentle reminder)
    public string AfternoonReminderTitle => "Én lille handling beviser hvem du er";
    public string AfternoonReminderBody => "Du har ikke fuldført din daglige forpligtelse endnu. Der er stadig tid til at vise hvem du vil være.";
    
    // Daily Identity Commitment - Evening (Reflection prompt)
    public string EveningReflectionTitle => "Var du den du ønskede at være i dag?";
    public string EveningReflectionBody => "Tag et øjeblik til at reflektere over din dag og fejre enhver fremgang, uanset hvor lille.";
}
