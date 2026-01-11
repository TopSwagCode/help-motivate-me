using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization.EmailTemplates;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace HelpMotivateMe.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendLoginLinkAsync(string email, string loginUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = templates.LoginLinkSubject;

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetLoginLinkHtmlBody(loginUrl),
            TextBody = templates.GetLoginLinkTextBody(loginUrl)
        };

        message.Body = builder.ToMessageBody();

        await SendMessageAsync(message);
    }

    public async Task SendBuddyInviteAsync(string email, string inviterName, string loginUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = templates.GetBuddyInviteSubject(inviterName);

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetBuddyInviteHtmlBody(inviterName, loginUrl),
            TextBody = templates.GetBuddyInviteTextBody(inviterName, loginUrl)
        };

        message.Body = builder.ToMessageBody();
        await SendMessageAsync(message);
    }

    public async Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle, string journalUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = templates.GetBuddyJournalSubject(buddyName);

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetBuddyJournalHtmlBody(buddyName, entryTitle, journalUrl),
            TextBody = templates.GetBuddyJournalTextBody(buddyName, entryTitle, journalUrl)
        };

        message.Body = builder.ToMessageBody();
        await SendMessageAsync(message);
    }

    public async Task SendWaitlistConfirmationAsync(string email, string name, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(name, email));
        message.Subject = templates.WaitlistSubject;

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetWaitlistHtmlBody(name),
            TextBody = templates.GetWaitlistTextBody(name)
        };

        message.Body = builder.ToMessageBody();
        await SendMessageAsync(message);
    }

    public async Task SendWhitelistInviteAsync(string email, string loginUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = templates.WhitelistSubject;

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetWhitelistHtmlBody(loginUrl),
            TextBody = templates.GetWhitelistTextBody(loginUrl)
        };

        message.Body = builder.ToMessageBody();
        await SendMessageAsync(message);
    }

    private async Task SendMessageAsync(MimeMessage message)
    {
        using var client = new SmtpClient();

        var host = _configuration["Email:SmtpHost"] ?? "localhost";
        var port = int.Parse(_configuration["Email:SmtpPort"] ?? "1025");

        await client.ConnectAsync(host, port, false);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
