namespace HelpMotivateMe.Core.Interfaces;

public interface IEmailService
{
    Task SendLoginLinkAsync(string email, string loginUrl);
    Task SendBuddyInviteAsync(string email, string inviterName, string loginUrl);
    Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle, string journalUrl);
}
