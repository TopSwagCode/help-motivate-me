namespace HelpMotivateMe.Core.DTOs.Goals;

public record GoalResponse(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? TargetDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    int SortOrder,
    int TaskCount,
    int CompletedTaskCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
