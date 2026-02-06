namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public interface IEmailTemplates
{
    // Login Link
    string LoginLinkSubject { get; }

    // Email Verification
    string VerificationSubject { get; }

    // Waitlist Confirmation
    string WaitlistSubject { get; }

    // Whitelist Invite
    string WhitelistSubject { get; }
    string GetLoginLinkHtmlBody(string loginUrl);
    string GetLoginLinkTextBody(string loginUrl);
    string GetVerificationHtmlBody(string verificationUrl);
    string GetVerificationTextBody(string verificationUrl);

    // Buddy Invite
    string GetBuddyInviteSubject(string inviterName);
    string GetBuddyInviteHtmlBody(string inviterName, string loginUrl);
    string GetBuddyInviteTextBody(string inviterName, string loginUrl);

    // Buddy Journal Notification
    string GetBuddyJournalSubject(string buddyName);
    string GetBuddyJournalHtmlBody(string buddyName, string entryTitle, string journalUrl);
    string GetBuddyJournalTextBody(string buddyName, string entryTitle, string journalUrl);
    string GetWaitlistHtmlBody(string name);
    string GetWaitlistTextBody(string name);
    string GetWhitelistHtmlBody(string loginUrl);
    string GetWhitelistTextBody(string loginUrl);
}