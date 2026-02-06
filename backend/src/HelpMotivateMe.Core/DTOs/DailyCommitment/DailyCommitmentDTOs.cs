using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.DailyCommitment;

// Request DTOs
public record CreateDailyCommitmentRequest(
    Guid IdentityId,
    string ActionDescription,
    Guid? LinkedHabitStackItemId,
    Guid? LinkedTaskId
);

// Response DTOs
public record DailyCommitmentResponse(
    Guid Id,
    DateOnly CommitmentDate,
    Guid IdentityId,
    string IdentityName,
    string? IdentityColor,
    string? IdentityIcon,
    string ActionDescription,
    Guid? LinkedHabitStackItemId,
    Guid? LinkedTaskId,
    DailyCommitmentStatus Status,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record IdentityOptionResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    double Score,
    bool IsRecommended
);

public record CommitmentOptionsResponse(
    IEnumerable<IdentityOptionResponse> Identities,
    Guid? RecommendedIdentityId,
    string DefaultMode
);

public record ActionSuggestion(
    string Description,
    string Type, // "habit" or "task"
    Guid? HabitStackItemId,
    Guid? TaskId
);

public record ActionSuggestionsResponse(
    IEnumerable<ActionSuggestion> Suggestions
);

// For Yesterday missed commitment info
public record YesterdayCommitmentResponse(
    bool WasMissed,
    string? IdentityName,
    string? ActionDescription
);