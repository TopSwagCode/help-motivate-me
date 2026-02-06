using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IMilestoneService _milestoneService;

    public AuthController(
        IAuthService authService,
        IEmailService emailService,
        IAnalyticsService analyticsService,
        IMilestoneService milestoneService)
    {
        _authService = authService;
        _emailService = emailService;
        _analyticsService = analyticsService;
        _milestoneService = milestoneService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!_authService.IsSignupAllowed())
        {
            var isWhitelisted = await _authService.IsEmailWhitelistedAsync(request.Email);
            if (!isWhitelisted)
            {
                return StatusCode(403, new { code = "signup_disabled", message = "Signups are currently disabled. Please join the waitlist." });
            }
        }

        if (await _authService.EmailExistsAsync(request.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var passwordHash = _authService.HashPassword(request.Password);
        var user = await _authService.CreateUserAsync(request.Email, passwordHash, request.DisplayName, isEmailVerified: false);

        var verificationToken = await _authService.CreateEmailVerificationTokenAsync(user.Id, request.Email);

        var frontendUrl = _authService.GetFrontendUrl();
        var verificationUrl = $"{frontendUrl}/auth/verify-email?token={verificationToken.Token}";
        await _emailService.SendVerificationEmailAsync(request.Email, verificationUrl, user.PreferredLanguage);

        await _analyticsService.LogEventAsync(user.Id, GetSessionId(), "UserRegistered");

        return Ok(new { message = "Please check your email to verify your account.", email = request.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.GetUserByEmailAsync(request.Email);

        if (user == null || user.PasswordHash == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        if (!user.IsEmailVerified)
        {
            return Unauthorized(new { code = "email_not_verified", message = "Please verify your email before logging in.", email = user.Email });
        }

        user = await _authService.GetUserWithExternalLoginsAsync(user.Id);
        await SignInUser(user);

        await _analyticsService.LogEventAsync(user.Id, GetSessionId(), "UserLoggedIn");
        await _milestoneService.RecordEventAsync(user.Id, "UserLoggedIn");

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
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        await _analyticsService.LogEventAsync(userId.Value, GetSessionId(), "SettingsPageLoaded");

        return Ok(MapToResponse(user));
    }

    [HttpGet("external/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var frontendUrl = _authService.GetFrontendUrl();
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

        var externalLogin = await _authService.GetExternalLoginAsync(provider, externalId);
        User? user = externalLogin?.User;

        if (externalLogin == null)
        {
            if (email != null)
            {
                user = await _authService.GetUserByEmailAsync(email);
            }

            if (user == null)
            {
                if (!_authService.IsSignupAllowed())
                {
                    var userEmail = email ?? $"{externalId}@{provider}.local";
                    var isWhitelisted = await _authService.IsEmailWhitelistedAsync(userEmail);
                    if (!isWhitelisted)
                    {
                        var frontendUrl = _authService.GetFrontendUrl();
                        var waitlistUrl = $"{frontendUrl}/waitlist?email={Uri.EscapeDataString(userEmail)}&name={Uri.EscapeDataString(name ?? "")}&provider={Uri.EscapeDataString(provider)}";
                        return Redirect(waitlistUrl);
                    }
                }

                user = await _authService.CreateUserAsync(
                    email ?? $"{externalId}@{provider}.local",
                    displayName: name,
                    isEmailVerified: true);
            }

            await _authService.LinkExternalLoginAsync(user.Id, provider, externalId, name);
        }

        user = await _authService.GetUserWithExternalLoginsAsync(user!.Id);
        await SignInUser(user);
        await _milestoneService.RecordEventAsync(user.Id, "UserLoggedIn");

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
        if (userId == null) return Unauthorized();

        if (!await _authService.CanRemoveLoginMethodAsync(userId.Value))
        {
            return BadRequest(new { message = "Cannot remove last login method. Set a password first." });
        }

        var success = await _authService.UnlinkExternalLoginAsync(userId.Value, provider);
        if (!success)
        {
            return NotFound(new { message = "External login not found" });
        }

        return NoContent();
    }

    [HttpPost("request-login-link")]
    public async Task<IActionResult> RequestLoginLink([FromBody] RequestLoginLinkRequest request)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _authService.GetUserByEmailAsync(email);

        if (user == null)
        {
            if (!_authService.IsSignupAllowed())
            {
                var isWhitelisted = await _authService.IsEmailWhitelistedAsync(email);
                if (!isWhitelisted)
                {
                    return StatusCode(403, new { code = "not_whitelisted", email = email, message = "You don't have an account yet. Please join the waitlist." });
                }
            }

            user = await _authService.CreateUserAsync(email, isEmailVerified: true);
        }

        if (!user.IsActive)
        {
            return Ok(new { message = "If an account exists, a login link has been sent." });
        }

        var loginToken = await _authService.CreateLoginTokenAsync(user.Id, email, 24);

        var frontendUrl = _authService.GetFrontendUrl();
        var loginUrl = $"{frontendUrl}/auth/login?token={loginToken.Token}";
        await _emailService.SendLoginLinkAsync(email, loginUrl, user.PreferredLanguage);

        return Ok(new { message = "If an account exists, a login link has been sent." });
    }

    [HttpPost("login-with-token")]
    public async Task<ActionResult<UserResponse>> LoginWithToken([FromBody] LoginWithTokenRequest request)
    {
        var token = await _authService.GetLoginTokenAsync(request.Token);

        if (token == null)
        {
            return Unauthorized(new { message = "Invalid or expired login link" });
        }

        if (token.IsUsed)
        {
            var gracePeriod = TimeSpan.FromMinutes(2);
            if (token.UsedAt == null || DateTime.UtcNow - token.UsedAt.Value > gracePeriod)
            {
                return Unauthorized(new { message = "This login link has already been used" });
            }
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "This login link has expired" });
        }

        if (!token.User.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        if (!token.IsUsed)
        {
            token.UsedAt = DateTime.UtcNow;
        }

        if (!token.User.IsEmailVerified)
        {
            token.User.IsEmailVerified = true;
        }

        await _authService.UpdateUserAsync(token.User);

        await SignInUser(token.User);
        await _milestoneService.RecordEventAsync(token.User.Id, "UserLoggedIn");

        return Ok(MapToResponse(token.User));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<UserResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var token = await _authService.GetEmailVerificationTokenAsync(request.Token);

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

        token.UsedAt = DateTime.UtcNow;
        token.User.IsEmailVerified = true;
        await _authService.UpdateUserAsync(token.User);

        await SignInUser(token.User);
        await _milestoneService.RecordEventAsync(token.User.Id, "UserLoggedIn");
        await _analyticsService.LogEventAsync(token.User.Id, GetSessionId(), "EmailVerified");

        return Ok(MapToResponse(token.User));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _authService.GetUserByEmailAsync(email);

        if (user == null || user.IsEmailVerified || !user.IsActive)
        {
            return Ok(new { message = "If an unverified account exists with this email, a verification link has been sent." });
        }

        var verificationToken = await _authService.CreateEmailVerificationTokenAsync(user.Id, email);

        var frontendUrl = _authService.GetFrontendUrl();
        var verificationUrl = $"{frontendUrl}/auth/verify-email?token={verificationToken.Token}";
        await _emailService.SendVerificationEmailAsync(email, verificationUrl, user.PreferredLanguage);

        return Ok(new { message = "If an unverified account exists with this email, a verification link has been sent." });
    }

    [HttpPatch("profile")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? null
            : request.DisplayName.Trim();

        await _authService.UpdateUserAsync(user);
        return Ok(MapToResponse(user));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound();

        if (user.PasswordHash == null)
        {
            return BadRequest(new { message = "Cannot change password. Account uses external authentication only." });
        }

        if (!_authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "New password must be at least 8 characters" });
        }

        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        await _authService.UpdateUserAsync(user);

        return NoContent();
    }

    [HttpPatch("membership")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateMembership([FromBody] UpdateMembershipRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        if (!Enum.TryParse<MembershipTier>(request.Tier, true, out var tier))
        {
            return BadRequest(new { message = "Invalid membership tier. Must be Free, Plus, or Pro." });
        }

        user.MembershipTier = tier;
        await _authService.UpdateUserAsync(user);

        return Ok(MapToResponse(user));
    }

    [HttpPost("complete-onboarding")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> CompleteOnboarding()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        user.HasCompletedOnboarding = true;
        await _authService.UpdateUserAsync(user);

        return Ok(MapToResponse(user));
    }

    [HttpPost("reset-onboarding")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> ResetOnboarding()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        user.HasCompletedOnboarding = false;
        await _authService.UpdateUserAsync(user);

        return Ok(MapToResponse(user));
    }

    [HttpPatch("language")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
        {
            return BadRequest(new { message = "Invalid language. Must be English or Danish." });
        }

        user.PreferredLanguage = language;
        await _authService.UpdateUserAsync(user);

        return Ok(MapToResponse(user));
    }

    [HttpGet("notification-preferences")]
    [Authorize]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetNotificationPreferences()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _authService.GetOrCreateNotificationPreferencesAsync(userId.Value);
        return Ok(MapToNotificationPreferencesResponse(prefs));
    }

    [HttpPatch("notification-preferences")]
    [Authorize]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _authService.UpdateNotificationPreferencesAsync(userId.Value, request);
        return Ok(MapToNotificationPreferencesResponse(prefs));
    }

    [HttpPost("login-with-buddy-token")]
    public async Task<ActionResult<BuddyLoginResponse>> LoginWithBuddyToken([FromBody] LoginWithTokenRequest request)
    {
        var token = await _authService.GetBuddyInviteTokenAsync(request.Token);

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

        token.UsedAt = DateTime.UtcNow;
        token.BuddyUser.HasCompletedOnboarding = true;
        await _authService.UpdateUserAsync(token.BuddyUser);

        await SignInUser(token.BuddyUser);
        await _milestoneService.RecordEventAsync(token.BuddyUser.Id, "UserLoggedIn");

        return Ok(new BuddyLoginResponse(
            MapToResponse(token.BuddyUser),
            token.InviterUserId,
            token.InviterUser.DisplayName ?? token.InviterUser.Email
        ));
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserWithExternalLoginsAsync(userId.Value);

        if (user.PasswordHash != null)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Password is required to delete your account" });
            }

            if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest(new { message = "Incorrect password" });
            }
        }

        await _authService.DeleteAccountAsync(userId.Value);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }

    private async Task SignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
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

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.Id,
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
