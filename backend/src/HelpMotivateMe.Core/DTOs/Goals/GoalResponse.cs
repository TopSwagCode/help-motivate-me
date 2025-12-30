using HelpMotivateMe.Core.DTOs.Categories;

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
    IEnumerable<CategoryResponse> Categories,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
