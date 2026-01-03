using System.Security.Claims;
using System.Security.Cryptography;
using HelpMotivateMe.Core.DTOs.Auth;
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
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthController(AppDbContext db, IConfiguration configuration, IEmailService emailService)
    {
        _db = db;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
    {
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
            DisplayName = request.DisplayName
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SignInUser(user);

        return Ok(MapToResponse(user));
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

        await SignInUser(user);

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
                // Create new user
                user = new User
                {
                    Username = await GenerateUniqueUsername(name ?? "user"),
                    Email = email ?? $"{externalId}@{provider}.local",
                    DisplayName = name
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
            // Auto-create account for new users
            user = new User
            {
                Username = await GenerateUniqueUsername(email.Split('@')[0]),
                Email = email
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

        // Send email
        await _emailService.SendLoginLinkAsync(email, loginUrl);

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

        if (token.IsUsed)
        {
            return Unauthorized(new { message = "This login link has already been used" });
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "This login link has expired" });
        }

        if (!token.User.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled" });
        }

        // Mark token as used
        token.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await SignInUser(token.User);

        return Ok(MapToResponse(token.User));
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
            user.Role.ToString()
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
}
