namespace HelpMotivateMe.Core.DTOs.Journal;

public record JournalReactionResponse(
    Guid Id,
    string Emoji,
    Guid UserId,
    string UserDisplayName,
    DateTime CreatedAt
);

public record AddJournalReactionRequest(
    string Emoji
);