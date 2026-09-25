using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Core.DTOs.Shared;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HelpMotivateMe.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.GetUserByUsernameAsync(request.Username);
        if (user is null || !user.IsActive || !_authService.VerifyPassword(user, request.Password))
            return Unauthorized(new MessageResponse("Invalid username or password"));

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
        var user = await FindCurrentUser();
        return user is null ? Unauthorized() : Ok(MapToResponse(user));
    }

    [HttpPatch("profile")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await FindCurrentUser();
        if (user is null) return Unauthorized();

        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        await _authService.UpdateUserAsync(user);
        return Ok(MapToResponse(user));
    }

    [HttpPost("complete-onboarding")]
    [Authorize]
    public Task<ActionResult<UserResponse>> CompleteOnboarding() => SetOnboardingState(true);

    [HttpPost("reset-onboarding")]
    [Authorize]
    public Task<ActionResult<UserResponse>> ResetOnboarding() => SetOnboardingState(false);

    [HttpPatch("language")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        var user = await FindCurrentUser();
        if (user is null) return Unauthorized();
        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return BadRequest(new MessageResponse("Invalid language. Must be English or Danish."));

        user.PreferredLanguage = language;
        await _authService.UpdateUserAsync(user);
        return Ok(MapToResponse(user));
    }

    private async Task<ActionResult<UserResponse>> SetOnboardingState(bool completed)
    {
        var user = await FindCurrentUser();
        if (user is null) return Unauthorized();

        user.HasCompletedOnboarding = completed;
        await _authService.UpdateUserAsync(user);
        return Ok(MapToResponse(user));
    }

    private async Task<User?> FindCurrentUser()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? await _authService.GetUserByIdAsync(userId) : null;
    }

    private async Task SignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("credential_version", user.CredentialVersion.ToString())
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private static UserResponse MapToResponse(User user) => new(
        user.Id,
        user.Username,
        user.DisplayName,
        user.CreatedAt,
        user.HasCompletedOnboarding,
        user.PreferredLanguage.ToString());
}
