using System.Security.Claims;
using System.Security.Cryptography;
using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string SessionIdKey = "AnalyticsSessionId";
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IAnalyticsService _analyticsService;

    public AuthController(AppDbContext db, IConfiguration configuration, IEmailService emailService, IAnalyticsService analyticsService)
    {
        _db = db;
        _configuration = configuration;
        _emailService = emailService;
        _analyticsService = analyticsService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if signups are allowed
        if (!IsSignupAllowed())
        {
            var isWhitelisted = await IsEmailWhitelisted(request.Email);
            if (!isWhitelisted)
            {
                return StatusCode(403, new { code = "signup_disabled", message = "Signups are currently disabled. Please join the waitlist." });
            }
        }

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.DisplayName,
            IsEmailVerified = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Generate verification token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = token,
            Email = request.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _db.EmailVerificationTokens.Add(verificationToken);
        await _db.SaveChangesAsync();

        // Send verification email
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var verificationUrl = $"{frontendUrl}/auth/verify-email?token={token}";
        await _emailService.SendVerificationEmailAsync(request.Email, verificationUrl, user.PreferredLanguage);

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(user.Id, sessionId, "UserRegistered");

        return Ok(new { message = "Please check your email to verify your account.", email = request.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

        if (user == null || user.PasswordHash == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        if (!user.IsEmailVerified)
        {
            return Unauthorized(new { code = "email_not_verified", message = "Please verify your email before logging in.", email = user.Email });
        }

        await SignInUser(user);

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(user.Id, sessionId, "UserLoggedIn");

        return Ok(MapToResponse(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(userId.Value, sessionId, "SettingsPageLoaded");

        return Ok(MapToResponse(user));
    }

    [HttpGet("external/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var redirectUrl = $"{frontendUrl}/auth/callback";
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/callback/{provider}?returnUrl={Uri.EscapeDataString(redirectUrl)}"
        };
        return Challenge(properties, provider);
    }

    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> ExternalCallback(string provider, string? returnUrl)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(provider);
        if (!authenticateResult.Succeeded)
        {
            return Redirect($"{returnUrl ?? "/auth/login"}?error=auth_failed");
        }

        var externalId = authenticateResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authenticateResult.Principal?.FindFirstValue(ClaimTypes.Email);
        var name = authenticateResult.Principal?.FindFirstValue(ClaimTypes.Name)
            ?? authenticateResult.Principal?.FindFirstValue("urn:github:login");

        if (string.IsNullOrEmpty(externalId))
        {
            return Redirect($"{returnUrl ?? "/auth/login"}?error=no_id");
        }

        // Check if this external login already exists
        var externalLogin = await _db.UserExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == externalId);

        User? user = null;

        if (externalLogin != null)
        {
            user = externalLogin.User;
        }
        else
        {
            // Check if user exists by email
            if (email != null)
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user == null)
            {
                // Check if signups are allowed for new users
                if (!IsSignupAllowed())
                {
                    var userEmail = email ?? $"{externalId}@{provider}.local";
                    var isWhitelisted = await IsEmailWhitelisted(userEmail);
                    if (!isWhitelisted)
                    {
                        // Redirect to waitlist page with user info
                        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
                        var waitlistUrl = $"{frontendUrl}/waitlist?email={Uri.EscapeDataString(userEmail)}&name={Uri.EscapeDataString(name ?? "")}&provider={Uri.EscapeDataString(provider)}";
                        return Redirect(waitlistUrl);
                    }
                }

                // Create new user - OAuth users are automatically verified since their email is verified by the provider
                user = new User
                {
                    Username = await GenerateUniqueUsername(name ?? "user"),
                    Email = email ?? $"{externalId}@{provider}.local",
                    DisplayName = name,
                    IsEmailVerified = true
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(); // Save to get the user ID
            }

            // Link external login to the user
            var newExternalLogin = new UserExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderKey = externalId,
                ProviderDisplayName = name
            };
            _db.UserExternalLogins.Add(newExternalLogin);
            await _db.SaveChangesAsync();
        }

        await SignInUser(user);

        return Redirect(returnUrl ?? "/dashboard");
    }

    [HttpPost("link/{provider}")]
    [Authorize]
    public IActionResult LinkExternalLogin(string provider)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/link-callback/{provider}"
        };
        return Challenge(properties, provider);
    }

    [HttpDelete("unlink/{provider}")]
    [Authorize]
    public async Task<IActionResult> UnlinkExternalLogin(string provider)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var externalLogin = await _db.UserExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider);

        if (externalLogin == null)
        {
            return NotFound(new { message = "External login not found" });
        }

        // Check if user has password or other login methods
        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstAsync(u => u.Id == userId);

        if (user.PasswordHash == null && user.ExternalLogins.Count <= 1)
        {
            return BadRequest(new { message = "Cannot remove last login method. Set a password first." });
        }

        _db.UserExternalLogins.Remove(externalLogin);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("request-login-link")]
    public async Task<IActionResult> RequestLoginLink([FromBody] RequestLoginLinkRequest request)
    {
        var email = request.Email.ToLowerInvariant();

        // Find or create user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Check if signups are allowed for new users
            if (!IsSignupAllowed())
            {
                var isWhitelisted = await IsEmailWhitelisted(email);
                if (!isWhitelisted)
                {
                    return StatusCode(403, new { code = "not_whitelisted", email = email, message = "You don't have an account yet. Please join the waitlist." });
                }
            }

            // Auto-create account for new users - email is verified since they're using magic link
            user = new User
            {
                Username = await GenerateUniqueUsername(email.Split('@')[0]),
                Email = email,
                IsEmailVerified = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        if (!user.IsActive)
        {
            // Don't reveal that account is disabled - just return success
            return Ok(new { message = "If an account exists, a login link has been sent." });
        }

        // Generate secure token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Get expiry hours from config (default 24)
        var expiryHours = int.Parse(_configuration["Auth:LoginLinkExpiryHours"] ?? "24");

        var loginToken = new EmailLoginToken
        {
            UserId = user.Id,
            Token = token,
            Email = email,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours)
        };

        _db.EmailLoginTokens.Add(loginToken);
        await _db.SaveChangesAsync();

        // Build login URL
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendUrl}/auth/login?token={token}";

        // Send email with user's language preference
        await _emailService.SendLoginLinkAsync(email, loginUrl, user.PreferredLanguage);

        return Ok(new { message = "If an account exists, a login link has been sent." });
    }

    [HttpPost("login-with-token")]
    public async Task<ActionResult<UserResponse>> LoginWithToken([FromBody] LoginWithTokenRequest request)
    {
        var token = await _db.EmailLoginTokens
            .Include(t => t.User)
                .ThenInclude(u => u.ExternalLogins)
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (token == null)
        {
            return Unauthorized(new { message = "Invalid or expired login link" });
        }

        // Allow token reuse within 2 minutes of first use (for PWA/browser switching)
        if (token.IsUsed)
        {
            var gracePeriod = TimeSpan.FromMinutes(2);
            if (token.UsedAt == null || DateTime.UtcNow - token.UsedAt.Value > gracePeriod)
            {
                return Unauthorized(new { message = "This login link has already been used" });
            }
            // Token is within grace period, allow reuse
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "This login link has expired" });
        }

        if (!token.User.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        // Mark token as used (only on first use)
        if (!token.IsUsed)
        {
            token.UsedAt = DateTime.UtcNow;
        }

        // Using a magic link also verifies the email
        if (!token.User.IsEmailVerified)
        {
            token.User.IsEmailVerified = true;
            token.User.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await SignInUser(token.User);

        return Ok(MapToResponse(token.User));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<UserResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var token = await _db.EmailVerificationTokens
            .Include(t => t.User)
                .ThenInclude(u => u.ExternalLogins)
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (token == null)
        {
            return BadRequest(new { message = "Invalid verification link" });
        }

        if (token.IsUsed)
        {
            return BadRequest(new { message = "This verification link has already been used" });
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "This verification link has expired" });
        }

        // Mark token as used
        token.UsedAt = DateTime.UtcNow;

        // Mark user as verified
        token.User.IsEmailVerified = true;
        token.User.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Auto-login the user
        await SignInUser(token.User);

        var sessionId = GetSessionId();
        await _analyticsService.LogEventAsync(token.User.Id, sessionId, "EmailVerified");

        return Ok(MapToResponse(token.User));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        // Always return success to prevent email enumeration
        if (user == null || user.IsEmailVerified || !user.IsActive)
        {
            return Ok(new { message = "If an unverified account exists with this email, a verification link has been sent." });
        }

        // Generate new verification token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = token,
            Email = email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _db.EmailVerificationTokens.Add(verificationToken);
        await _db.SaveChangesAsync();

        // Send verification email
        var frontendUrl = _configuration["FrontendUrl"] ?? _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173";
        var verificationUrl = $"{frontendUrl}/auth/verify-email?token={token}";
        await _emailService.SendVerificationEmailAsync(email, verificationUrl, user.PreferredLanguage);

        return Ok(new { message = "If an unverified account exists with this email, a verification link has been sent." });
    }

    [HttpPatch("profile")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? null
            : request.DisplayName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(user));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        if (user.PasswordHash == null)
        {
            return BadRequest(new { message = "Cannot change password. Account uses external authentication only." });
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "New password must be at least 8 characters" });
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("membership")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateMembership([FromBody] UpdateMembershipRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        if (!Enum.TryParse<MembershipTier>(request.Tier, true, out var tier))
        {
            return BadRequest(new { message = "Invalid membership tier. Must be Free, Plus, or Pro." });
        }

        user.MembershipTier = tier;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(user));
    }

    [HttpPost("complete-onboarding")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> CompleteOnboarding()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        user.HasCompletedOnboarding = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(user));
    }

    [HttpPost("reset-onboarding")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> ResetOnboarding()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        user.HasCompletedOnboarding = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(user));
    }

    [HttpPatch("language")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
        {
            return BadRequest(new { message = "Invalid language. Must be English or Danish." });
        }

        user.PreferredLanguage = language;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(user));
    }

    [HttpGet("notification-preferences")]
    [Authorize]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetNotificationPreferences()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _db.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (prefs == null)
        {
            // Create default preferences for this user
            prefs = new NotificationPreferences { UserId = userId.Value };
            _db.NotificationPreferences.Add(prefs);
            await _db.SaveChangesAsync();
        }

        return Ok(MapToNotificationPreferencesResponse(prefs));
    }

    [HttpPatch("notification-preferences")]
    [Authorize]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _db.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (prefs == null)
        {
            // Create default preferences if none exist
            prefs = new NotificationPreferences { UserId = userId.Value };
            _db.NotificationPreferences.Add(prefs);
        }

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
        return Ok(MapToNotificationPreferencesResponse(prefs));
    }

    [HttpPost("login-with-buddy-token")]
    public async Task<ActionResult<BuddyLoginResponse>> LoginWithBuddyToken([FromBody] LoginWithTokenRequest request)
    {
        var token = await _db.BuddyInviteTokens
            .Include(t => t.BuddyUser)
                .ThenInclude(u => u.ExternalLogins)
            .Include(t => t.InviterUser)
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (token == null)
        {
            return Unauthorized(new { message = "Invalid or expired invite link" });
        }

        if (token.UsedAt.HasValue)
        {
            return Unauthorized(new { message = "This invite link has already been used" });
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "This invite link has expired" });
        }

        if (!token.BuddyUser.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        // Mark token as used
        token.UsedAt = DateTime.UtcNow;

        // Mark onboarding as complete for the buddy (skip onboarding for buddy login)
        token.BuddyUser.HasCompletedOnboarding = true;
        token.BuddyUser.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await SignInUser(token.BuddyUser);

        return Ok(new BuddyLoginResponse(
            MapToResponse(token.BuddyUser),
            token.InviterUserId,
            token.InviterUser.DisplayName ?? token.InviterUser.Username
        ));
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        // If user has a password, require password verification
        if (user.PasswordHash != null)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Password is required to delete your account" });
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest(new { message = "Incorrect password" });
            }
        }

        // Delete related entities (many are configured with cascade delete, but handle explicitly for clarity)
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

        // Delete external logins
        _db.UserExternalLogins.RemoveRange(user.ExternalLogins);

        // Delete the user (cascade should handle goals, identities, habit stacks, etc.)
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();

        // Sign out the user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }

    private async Task SignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private Guid GetSessionId()
    {
        var sessionIdString = HttpContext.Session.GetString(SessionIdKey);
        if (sessionIdString != null && Guid.TryParse(sessionIdString, out var sessionId))
        {
            return sessionId;
        }

        var newSessionId = Guid.NewGuid();
        HttpContext.Session.SetString(SessionIdKey, newSessionId.ToString());
        return newSessionId;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.CreatedAt,
            user.ExternalLogins.Select(e => e.Provider),
            user.PasswordHash != null,
            user.MembershipTier.ToString(),
            user.HasCompletedOnboarding,
            user.Role.ToString(),
            user.PreferredLanguage.ToString()
        );
    }

    private async Task<string> GenerateUniqueUsername(string baseName)
    {
        var username = baseName.ToLowerInvariant().Replace(" ", "");
        var exists = await _db.Users.AnyAsync(u => u.Username == username);

        if (!exists)
        {
            return username;
        }

        var suffix = 1;
        while (await _db.Users.AnyAsync(u => u.Username == $"{username}{suffix}"))
        {
            suffix++;
        }

        return $"{username}{suffix}";
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
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

    private bool IsSignupAllowed()
    {
        // Default to true if not configured
        return !bool.TryParse(_configuration["Auth:AllowSignups"], out var allowed) || allowed;
    }

    private async Task<bool> IsEmailWhitelisted(string email)
    {
        return await _db.WhitelistEntries.AnyAsync(w => w.Email.ToLower() == email.ToLower());
    }

    private static NotificationPreferencesResponse MapToNotificationPreferencesResponse(NotificationPreferences prefs)
    {
        return new NotificationPreferencesResponse(
            prefs.NotificationsEnabled,
            prefs.EmailEnabled,
            prefs.SmsEnabled,
            prefs.PhoneEnabled,
            prefs.HabitRemindersEnabled,
            prefs.GoalRemindersEnabled,
            prefs.DailyDigestEnabled,
            prefs.StreakAlertsEnabled,
            prefs.MotivationalQuotesEnabled,
            prefs.WeeklyReviewEnabled,
            prefs.BuddyUpdatesEnabled,
            prefs.DailyCommitmentEnabled,
            prefs.CommitmentDefaultMode,
            (int)prefs.SelectedDays,
            prefs.PreferredTimeSlot.ToString(),
            prefs.CustomTimeStart?.ToString("HH:mm"),
            prefs.CustomTimeEnd?.ToString("HH:mm"),
            prefs.TimezoneId,
            prefs.UtcOffsetMinutes
        );
    }
}
