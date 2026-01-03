namespace HelpMotivateMe.Core.DTOs.Journal;

public record JournalEntryResponse(
    Guid Id,
    string Title,
    string? Description,
    DateOnly EntryDate,
    Guid? HabitStackId,
    string? HabitStackName,
    Guid? TaskItemId,
    string? TaskItemTitle,
    Guid? AuthorUserId,
    string? AuthorDisplayName,
    IEnumerable<JournalImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record JournalImageResponse(
    Guid Id,
    string FileName,
    string Url,
    int SortOrder
);
