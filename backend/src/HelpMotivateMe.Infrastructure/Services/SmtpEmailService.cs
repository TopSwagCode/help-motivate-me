using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization.EmailTemplates;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace HelpMotivateMe.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendLoginLinkAsync(string email, string loginUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
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

    public async Task SendVerificationEmailAsync(string email, string verificationUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = templates.VerificationSubject;

        var builder = new BodyBuilder
        {
            HtmlBody = templates.GetVerificationHtmlBody(verificationUrl),
            TextBody = templates.GetVerificationTextBody(verificationUrl)
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
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
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

    public async Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle,
        string journalUrl, Language language)
    {
        var templates = EmailTemplateProvider.GetTemplates(language);

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
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
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
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
            _configuration["Email:FromEmail"] ?? "noreply@helpmotivateme.local"
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
        var username = _configuration["Email:SmtpUsername"];
        var password = _configuration["Email:SmtpPassword"];

        try
        {
            _logger.LogInformation("Connecting to SMTP server {Host}:{Port}", host, port);

            // Use StartTls for port 587, SslOnConnect for port 465, no SSL for local dev
            var secureSocketOptions = port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None
            };

            await client.ConnectAsync(host, port, secureSocketOptions);
            _logger.LogInformation("Connected to SMTP server");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _logger.LogInformation("Authenticating with username: {Username}", username);
                await client.AuthenticateAsync(username, password);
                _logger.LogInformation("Authentication successful");
            }

            _logger.LogInformation("Sending email to {To}, Subject: {Subject}", message.To, message.Subject);
            var response = await client.SendAsync(message);
            _logger.LogInformation("Email sent successfully. Server response: {Response}", response);

            await client.DisconnectAsync(true);
            _logger.LogInformation("Disconnected from SMTP server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}. Host: {Host}, Port: {Port}", message.To, host, port);
            throw;
        }
    }
}