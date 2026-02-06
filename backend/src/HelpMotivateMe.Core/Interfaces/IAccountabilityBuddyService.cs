using HelpMotivateMe.Core.DTOs.Buddies;

namespace HelpMotivateMe.Core.Interfaces;

public interface IAccountabilityBuddyService
{
    Task<List<BuddyInfo>> GetMyBuddiesAsync(Guid userId);
    Task<List<BuddyForInfo>> GetBuddyingForAsync(Guid userId);
    Task<bool> IsBuddyForAsync(Guid buddyUserId, Guid targetUserId);
    Task<InviteBuddyResult> InviteBuddyAsync(Guid inviterUserId, string email);
    Task<bool> RemoveBuddyAsync(Guid userId, Guid buddyRelationshipId);
    Task<bool> LeaveBuddyAsync(Guid buddyUserId, Guid ownerUserId);
    Task<BuddyTodayViewData> GetBuddyTodayViewAsync(Guid targetUserId, DateOnly targetDate);
    Task<List<BuddyJournalEntryData>> GetBuddyJournalAsync(Guid targetUserId);
    Task<BuddyJournalEntryData> CreateBuddyJournalEntryAsync(
        Guid authorUserId,
        Guid targetUserId,
        string title,
        string? description,
        string? entryDate);
    Task<UploadImageResult> UploadBuddyJournalImageAsync(
        Guid authorUserId,
        Guid targetUserId,
        Guid entryId,
        Stream imageStream,
        string fileName,
        string contentType,
        long fileSize);
    Task<AddReactionResult> AddBuddyJournalReactionAsync(Guid userId, Guid targetUserId, Guid entryId, string emoji);
    Task<bool> RemoveBuddyJournalReactionAsync(Guid userId, Guid entryId, Guid reactionId);
}
