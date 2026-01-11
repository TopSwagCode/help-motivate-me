using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Interfaces;

public interface IEmailService
{
    Task SendLoginLinkAsync(string email, string loginUrl, Language language = Language.English);
    Task SendBuddyInviteAsync(string email, string inviterName, string loginUrl, Language language = Language.English);
    Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle, string journalUrl, Language language = Language.English);
    Task SendWaitlistConfirmationAsync(string email, string name, Language language = Language.English);
    Task SendWhitelistInviteAsync(string email, string loginUrl, Language language = Language.English);
}
