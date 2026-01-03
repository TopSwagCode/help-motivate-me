using HelpMotivateMe.Core.Interfaces;
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

    public async Task SendLoginLinkAsync(string email, string loginUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = "Your Login Link - Help Motivate Me";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4F46E5;'>Login to Help Motivate Me</h1>
                    <p>Click the button below to log in to your account. This link will expire in 24 hours and can only be used once.</p>
                    <p style='margin: 30px 0;'>
                        <a href='{loginUrl}'
                           style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                            Log In
                        </a>
                    </p>
                    <p style='color: #666; font-size: 14px;'>
                        If you didn't request this login link, you can safely ignore this email.
                    </p>
                    <p style='color: #666; font-size: 14px;'>
                        If the button doesn't work, copy and paste this link into your browser:<br/>
                        <a href='{loginUrl}' style='color: #4F46E5;'>{loginUrl}</a>
                    </p>
                </body>
                </html>",
            TextBody = $@"Login to Help Motivate Me

Click the link below to log in to your account. This link will expire in 24 hours and can only be used once.

{loginUrl}

If you didn't request this login link, you can safely ignore this email."
        };

        message.Body = builder.ToMessageBody();

        await SendMessageAsync(message);
    }

    public async Task SendBuddyInviteAsync(string email, string inviterName, string loginUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = $"{inviterName} wants you as their accountability buddy!";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4F46E5;'>You've Been Invited as an Accountability Buddy!</h1>

                    <p><strong>{inviterName}</strong> has invited you to be their accountability buddy on Help Motivate Me.</p>

                    <h2 style='color: #374151; font-size: 18px;'>What is an Accountability Buddy?</h2>
                    <p>An accountability buddy helps someone stay on track with their goals and habits. As an accountability buddy, you can:</p>
                    <ul>
                        <li>View their daily progress (habits, tasks, and goals)</li>
                        <li>Leave encouraging notes in their journal</li>
                        <li>Help them stay motivated on their journey</li>
                    </ul>

                    <h2 style='color: #374151; font-size: 18px;'>How to Be a Great Accountability Buddy</h2>
                    <ul>
                        <li>Check in regularly to see their progress</li>
                        <li>Celebrate their wins, no matter how small</li>
                        <li>Offer encouragement when they're struggling</li>
                        <li>Be supportive, not judgmental</li>
                    </ul>

                    <p style='margin: 30px 0;'>
                        <a href='{loginUrl}'
                           style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                            Accept Invitation & View Their Progress
                        </a>
                    </p>

                    <p style='color: #666; font-size: 14px;'>
                        This link will expire in 7 days. Click it to log in and see {inviterName}'s progress.
                    </p>
                    <p style='color: #666; font-size: 14px;'>
                        If the button doesn't work, copy and paste this link into your browser:<br/>
                        <a href='{loginUrl}' style='color: #4F46E5;'>{loginUrl}</a>
                    </p>
                </body>
                </html>",
            TextBody = $@"You've Been Invited as an Accountability Buddy!

{inviterName} has invited you to be their accountability buddy on Help Motivate Me.

What is an Accountability Buddy?
An accountability buddy helps someone stay on track with their goals and habits. As an accountability buddy, you can:
- View their daily progress (habits, tasks, and goals)
- Leave encouraging notes in their journal
- Help them stay motivated on their journey

How to Be a Great Accountability Buddy:
- Check in regularly to see their progress
- Celebrate their wins, no matter how small
- Offer encouragement when they're struggling
- Be supportive, not judgmental

Click here to accept the invitation and view their progress:
{loginUrl}

This link will expire in 7 days."
        };

        message.Body = builder.ToMessageBody();
        await SendMessageAsync(message);
    }

    public async Task SendBuddyJournalNotificationAsync(string email, string buddyName, string entryTitle, string journalUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "Help Motivate Me",
            _configuration["Email:FromAddress"] ?? "noreply@helpmotivateme.local"
        ));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = $"{buddyName} left you an encouraging note!";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4F46E5;'>New Journal Entry from Your Buddy!</h1>

                    <p>Your accountability buddy <strong>{buddyName}</strong> has written in your journal:</p>

                    <div style='background-color: #F3F4F6; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                        <p style='font-style: italic; margin: 0;'>""{entryTitle}""</p>
                    </div>

                    <p style='margin: 30px 0;'>
                        <a href='{journalUrl}'
                           style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                            View Full Entry
                        </a>
                    </p>

                    <p style='color: #666; font-size: 14px;'>
                        Keep up the great work! Your buddy is cheering you on.
                    </p>
                </body>
                </html>",
            TextBody = $@"New Journal Entry from Your Buddy!

Your accountability buddy {buddyName} has written in your journal:

""{entryTitle}""

View the full entry here:
{journalUrl}

Keep up the great work! Your buddy is cheering you on."
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
