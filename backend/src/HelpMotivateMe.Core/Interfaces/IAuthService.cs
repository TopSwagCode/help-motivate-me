using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Interfaces;

public interface IAuthService
{
    // Password operations
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
    
    // Token generation
    string GenerateSecureToken();
    
    // User operations
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User> GetUserWithExternalLoginsAsync(Guid userId);
    Task<bool> EmailExistsAsync(string email);
    Task<User> CreateUserAsync(string email, string? passwordHash = null, string? displayName = null, bool isEmailVerified = false);
    Task UpdateUserAsync(User user);
    
    // Email verification
    Task<EmailVerificationToken> CreateEmailVerificationTokenAsync(Guid userId, string email);
    Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token);
    
    // Login tokens (magic links)
    Task<EmailLoginToken> CreateLoginTokenAsync(Guid userId, string email, int expiryHours = 24);
    Task<EmailLoginToken?> GetLoginTokenAsync(string token);
    
    // External logins
    Task<UserExternalLogin?> GetExternalLoginAsync(string provider, string providerKey);
    Task<UserExternalLogin> LinkExternalLoginAsync(Guid userId, string provider, string providerKey, string? displayName = null);
    Task<bool> UnlinkExternalLoginAsync(Guid userId, string provider);
    Task<bool> CanRemoveLoginMethodAsync(Guid userId);
    
    // Whitelist and signup checks
    Task<bool> IsEmailWhitelistedAsync(string email);
    bool IsSignupAllowed();
    
    // Notification preferences
    Task<NotificationPreferences> GetOrCreateNotificationPreferencesAsync(Guid userId);
    Task<NotificationPreferences> UpdateNotificationPreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request);

    // Account deletion
    Task DeleteAccountAsync(Guid userId);
    
    // URL building
    string GetFrontendUrl();
}
