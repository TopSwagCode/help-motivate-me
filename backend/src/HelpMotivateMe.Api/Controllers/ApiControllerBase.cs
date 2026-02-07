using System.Security.Claims;
using HelpMotivateMe.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

/// <summary>
///     Base controller providing common functionality for all API controllers.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected const string SessionIdKey = "AnalyticsSessionId";

    /// <summary>
    ///     Gets the authenticated user's ID from claims.
    /// </summary>
    [Obsolete("Use IResourceAuthorizationService.GetCurrentUserId() instead.")]
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in claims");
        return userId;
    }

    /// <summary>
    ///     Gets or creates an analytics session ID for the current request.
    /// </summary>
    protected Guid GetSessionId()
    {
        var sessionIdString = HttpContext.Session.GetString(SessionIdKey);
        if (sessionIdString != null && Guid.TryParse(sessionIdString, out var sessionId)) return sessionId;

        var newSessionId = Guid.NewGuid();
        HttpContext.Session.SetString(SessionIdKey, newSessionId.ToString());
        return newSessionId;
    }

    /// <summary>
    ///     Gets the authenticated user's role from claims.
    /// </summary>
    protected UserRole GetUserRole()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (roleClaim != null && Enum.TryParse<UserRole>(roleClaim, out var role)) return role;
        return UserRole.User;
    }
}