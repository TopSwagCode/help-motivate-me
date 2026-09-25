using System.Security.Claims;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HelpMotivateMe.Infrastructure.Services;

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResourceAuthorizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in claims");
        return userId;
    }

    public bool IsOwner(Guid resourceOwnerId)
    {
        return resourceOwnerId == GetCurrentUserId();
    }
}
