using HelpMotivateMe.Core.Entities;

namespace HelpMotivateMe.Core.Interfaces;

public interface IAuthService
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(Guid userId);
    bool VerifyPassword(User user, string password);
    Task UpdateUserAsync(User user);
}
