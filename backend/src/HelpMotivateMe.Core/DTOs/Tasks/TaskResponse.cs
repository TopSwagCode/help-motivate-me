using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record TaskResponse(
    Guid Id,
    Guid GoalId,
    Guid? ParentTaskId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateOnly? CompletedAt,
    int SortOrder,
    IEnumerable<TaskResponse> Subtasks,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    string? IdentityColor,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
