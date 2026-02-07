using System.Security.Claims;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IQueryInterface<AccountabilityBuddy> _buddies;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResourceAuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IQueryInterface<AccountabilityBuddy> buddies)
    {
        _httpContextAccessor = httpContextAccessor;
        _buddies = buddies;
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

    public async Task<bool> IsOwnerOrBuddyAsync(Guid resourceOwnerId)
    {
        var currentUserId = GetCurrentUserId();

        if (resourceOwnerId == currentUserId)
            return true;

        // Check if current user is a buddy of the resource owner
        return await _buddies.AnyAsync(b =>
            b.UserId == resourceOwnerId && b.BuddyUserId == currentUserId);
    }
}
