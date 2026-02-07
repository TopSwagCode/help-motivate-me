namespace HelpMotivateMe.Core.Interfaces;

public interface IResourceAuthorizationService
{
    Guid GetCurrentUserId();
    bool IsOwner(Guid resourceOwnerId);
    Task<bool> IsOwnerOrBuddyAsync(Guid resourceOwnerId);
}
