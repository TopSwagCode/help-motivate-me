using System.Security.Cryptography;
using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpMotivateMe.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    #region Password Operations

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    #endregion

    #region Token Generation

    public string GenerateSecureToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    #endregion

    #region User Operations

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User> GetUserWithExternalLoginsAsync(Guid userId)
    {
        return await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstAsync(u => u.Id == userId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> CreateUserAsync(string email, string? passwordHash = null, string? displayName = null, bool isEmailVerified = false)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            DisplayName = displayName,
            IsEmailVerified = isEmailVerified
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    #endregion

    #region Email Verification

    public async Task<EmailVerificationToken> CreateEmailVerificationTokenAsync(Guid userId, string email)
    {
        var token = GenerateSecureToken();

        var verificationToken = new EmailVerificationToken
        {
            UserId = userId,
            Token = token,
            Email = email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _db.EmailVerificationTokens.Add(verificationToken);
        await _db.SaveChangesAsync();
        return verificationToken;
    }

    public async Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token)
    {
        return await _db.EmailVerificationTokens
            .Include(t => t.User)
                .ThenInclude(u => u.ExternalLogins)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    #endregion

    #region Login Tokens (Magic Links)

    public async Task<EmailLoginToken> CreateLoginTokenAsync(Guid userId, string email, int expiryHours = 24)
    {
        var token = GenerateSecureToken();

        var loginToken = new EmailLoginToken
        {
            UserId = userId,
            Token = token,
            Email = email,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours)
        };

        _db.EmailLoginTokens.Add(loginToken);
        await _db.SaveChangesAsync();
        return loginToken;
    }

    public async Task<EmailLoginToken?> GetLoginTokenAsync(string token)
    {
        return await _db.EmailLoginTokens
            .Include(t => t.User)
                .ThenInclude(u => u.ExternalLogins)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    #endregion

    #region External Logins

    public async Task<UserExternalLogin?> GetExternalLoginAsync(string provider, string providerKey)
    {
        return await _db.UserExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey);
    }

    public async Task<UserExternalLogin> LinkExternalLoginAsync(Guid userId, string provider, string providerKey, string? displayName = null)
    {
        var externalLogin = new UserExternalLogin
        {
            UserId = userId,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = displayName
        };

        _db.UserExternalLogins.Add(externalLogin);
        await _db.SaveChangesAsync();
        return externalLogin;
    }

    public async Task<bool> UnlinkExternalLoginAsync(Guid userId, string provider)
    {
        var externalLogin = await _db.UserExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider);

        if (externalLogin == null)
        {
            return false;
        }

        _db.UserExternalLogins.Remove(externalLogin);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanRemoveLoginMethodAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstAsync(u => u.Id == userId);

        // Can remove if user has password AND at least one remaining login method,
        // or has more than one external login
        return user.PasswordHash != null || user.ExternalLogins.Count > 1;
    }

    #endregion

    #region Whitelist and Signup Checks

    public async Task<bool> IsEmailWhitelistedAsync(string email)
    {
        return await _db.WhitelistEntries.AnyAsync(w => w.Email.ToLower() == email.ToLower());
    }

    public bool IsSignupAllowed()
    {
        // Default to true if not configured
        return !bool.TryParse(_configuration["Auth:AllowSignups"], out var allowed) || allowed;
    }

    #endregion

    #region Notification Preferences

    public async Task<NotificationPreferences> GetOrCreateNotificationPreferencesAsync(Guid userId)
    {
        var prefs = await _db.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (prefs == null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            _db.NotificationPreferences.Add(prefs);
            await _db.SaveChangesAsync();
        }

        return prefs;
    }

    public async Task<NotificationPreferences> UpdateNotificationPreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request)
    {
        var prefs = await GetOrCreateNotificationPreferencesAsync(userId);

        // Update only the fields that were provided (partial update for auto-save)
        if (request.NotificationsEnabled.HasValue)
            prefs.NotificationsEnabled = request.NotificationsEnabled.Value;
        if (request.EmailEnabled.HasValue)
            prefs.EmailEnabled = request.EmailEnabled.Value;
        if (request.SmsEnabled.HasValue)
            prefs.SmsEnabled = request.SmsEnabled.Value;
        if (request.HabitRemindersEnabled.HasValue)
            prefs.HabitRemindersEnabled = request.HabitRemindersEnabled.Value;
        if (request.GoalRemindersEnabled.HasValue)
            prefs.GoalRemindersEnabled = request.GoalRemindersEnabled.Value;
        if (request.DailyDigestEnabled.HasValue)
            prefs.DailyDigestEnabled = request.DailyDigestEnabled.Value;
        if (request.StreakAlertsEnabled.HasValue)
            prefs.StreakAlertsEnabled = request.StreakAlertsEnabled.Value;
        if (request.MotivationalQuotesEnabled.HasValue)
            prefs.MotivationalQuotesEnabled = request.MotivationalQuotesEnabled.Value;
        if (request.WeeklyReviewEnabled.HasValue)
            prefs.WeeklyReviewEnabled = request.WeeklyReviewEnabled.Value;
        if (request.BuddyUpdatesEnabled.HasValue)
            prefs.BuddyUpdatesEnabled = request.BuddyUpdatesEnabled.Value;
        if (request.DailyCommitmentEnabled.HasValue)
            prefs.DailyCommitmentEnabled = request.DailyCommitmentEnabled.Value;
        if (request.CommitmentDefaultMode != null)
            prefs.CommitmentDefaultMode = request.CommitmentDefaultMode;
        if (request.SelectedDays.HasValue)
            prefs.SelectedDays = (NotificationDays)request.SelectedDays.Value;
        if (request.PreferredTimeSlot != null && Enum.TryParse<TimeSlot>(request.PreferredTimeSlot, true, out var timeSlot))
            prefs.PreferredTimeSlot = timeSlot;
        if (request.CustomTimeStart != null)
            prefs.CustomTimeStart = TimeOnly.TryParse(request.CustomTimeStart, out var startTime) ? startTime : null;
        if (request.CustomTimeEnd != null)
            prefs.CustomTimeEnd = TimeOnly.TryParse(request.CustomTimeEnd, out var endTime) ? endTime : null;
        if (request.TimezoneId != null)
            prefs.TimezoneId = request.TimezoneId;
        if (request.UtcOffsetMinutes.HasValue)
            prefs.UtcOffsetMinutes = request.UtcOffsetMinutes.Value;

        prefs.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return prefs;
    }

    #endregion

    #region Account Deletion

    public async Task DeleteAccountAsync(Guid userId)
    {
        // Delete notification preferences
        var notificationPrefs = await _db.NotificationPreferences.FirstOrDefaultAsync(np => np.UserId == userId);
        if (notificationPrefs != null)
            _db.NotificationPreferences.Remove(notificationPrefs);

        // Delete push subscriptions
        var pushSubscriptions = await _db.PushSubscriptions.Where(ps => ps.UserId == userId).ToListAsync();
        _db.PushSubscriptions.RemoveRange(pushSubscriptions);

        // Delete email login tokens
        var emailLoginTokens = await _db.EmailLoginTokens.Where(t => t.UserId == userId).ToListAsync();
        _db.EmailLoginTokens.RemoveRange(emailLoginTokens);

        // Delete email verification tokens
        var verificationTokens = await _db.EmailVerificationTokens.Where(t => t.UserId == userId).ToListAsync();
        _db.EmailVerificationTokens.RemoveRange(verificationTokens);

        // Delete the user (cascade should handle goals, identities, habit stacks, etc.)
        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstAsync(u => u.Id == userId);

        _db.UserExternalLogins.RemoveRange(user.ExternalLogins);
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();
    }

    #endregion

    #region URL Building

    public string GetFrontendUrl()
    {
        return _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
    }

    #endregion
}
