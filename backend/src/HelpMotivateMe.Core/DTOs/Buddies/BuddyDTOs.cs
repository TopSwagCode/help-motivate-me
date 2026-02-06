using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.DTOs.Today;

namespace HelpMotivateMe.Core.DTOs.Buddies;

// Result types
public record InviteBuddyResult(bool Success, string? ErrorMessage, BuddyInfo? Buddy);
public record UploadImageResult(bool Success, string? ErrorMessage, BuddyJournalImageData? Image);
public record AddReactionResult(bool Success, string? ErrorMessage, BuddyJournalReactionData? Reaction);

// Data types for service layer
public record BuddyInfo(Guid Id, Guid BuddyUserId, string Email, string DisplayName, DateTime CreatedAt);
public record BuddyForInfo(Guid Id, Guid UserId, string Email, string DisplayName, DateTime CreatedAt);
public record BuddyTodayViewData(
    Guid UserId,
    string UserDisplayName,
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback
);
public record BuddyJournalEntryData(
    Guid Id,
    string Title,
    string? Description,
    string EntryDate,
    Guid? AuthorUserId,
    string? AuthorDisplayName,
    List<BuddyJournalImageData> Images,
    List<BuddyJournalReactionData> Reactions,
    DateTime CreatedAt
);
public record BuddyJournalImageData(Guid Id, string FileName, string Url, int SortOrder);
public record BuddyJournalReactionData(Guid Id, string Emoji, Guid UserId, string UserDisplayName, DateTime CreatedAt);
