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

// API Response DTOs

public record BuddyRelationshipsResponse(
    List<BuddyResponse> MyBuddies,
    List<BuddyForResponse> BuddyingFor
);

public record BuddyResponse(
    Guid Id,
    Guid BuddyUserId,
    string BuddyEmail,
    string BuddyDisplayName,
    DateTime CreatedAt
);

public record BuddyForResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    DateTime CreatedAt
);

public record InviteBuddyRequest(string Email);

public record BuddyTodayViewResponse(
    Guid UserId,
    string UserDisplayName,
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback
);

public record BuddyJournalEntryResponse(
    Guid Id,
    string Title,
    string? Description,
    string EntryDate,
    Guid? AuthorUserId,
    string? AuthorDisplayName,
    List<BuddyJournalImageResponse> Images,
    List<BuddyJournalReactionResponse> Reactions,
    DateTime CreatedAt
);

public record BuddyJournalImageResponse(
    Guid Id,
    string FileName,
    string Url,
    int SortOrder
);

public record BuddyJournalReactionResponse(
    Guid Id,
    string Emoji,
    Guid UserId,
    string UserDisplayName,
    DateTime CreatedAt
);

public record AddBuddyJournalReactionRequest(
    string Emoji
);

public record CreateBuddyJournalEntryRequest(
    string Title,
    string? Description,
    string? EntryDate
);