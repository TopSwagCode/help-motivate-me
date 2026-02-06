using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Localization.PushNotifications;

public static class PushNotificationMessageProvider
{
    private static readonly Dictionary<Language, IPushNotificationMessages> Messages = new()
    {
        { Language.English, new EnglishPushNotificationMessages() },
        { Language.Danish, new DanishPushNotificationMessages() }
    };

    public static IPushNotificationMessages GetMessages(Language language)
    {
        return Messages.GetValueOrDefault(language, Messages[Language.English]);
    }
}