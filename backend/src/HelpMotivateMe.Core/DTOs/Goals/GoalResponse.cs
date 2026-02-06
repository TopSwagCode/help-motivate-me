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
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityColor,
    string? IdentityIcon,
    DateTime CreatedAt,
    DateTime UpdatedAt
);