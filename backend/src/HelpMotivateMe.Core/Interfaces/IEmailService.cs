namespace HelpMotivateMe.Core.Interfaces;

public interface IEmailService
{
    Task SendLoginLinkAsync(string email, string loginUrl);
}
