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

        using var client = new SmtpClient();

        var host = _configuration["Email:SmtpHost"] ?? "localhost";
        var port = int.Parse(_configuration["Email:SmtpPort"] ?? "1025");

        await client.ConnectAsync(host, port, false);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
