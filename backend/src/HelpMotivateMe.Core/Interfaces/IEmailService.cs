using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Interfaces;

public interface IEmailService
{
    Task SendLoginLinkAsync(string email, string loginUrl, Language language);
    Task SendVerificationEmailAsync(string email, string verificationUrl, Language language);
    Task SendBuddyInviteAsync(string email, string inviterName, string loginUrl, Language language);

    Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle, string journalUrl,
        Language language);

    Task SendWaitlistConfirmationAsync(string email, string name, Language language);
    Task SendWhitelistInviteAsync(string email, string loginUrl, Language language);
}