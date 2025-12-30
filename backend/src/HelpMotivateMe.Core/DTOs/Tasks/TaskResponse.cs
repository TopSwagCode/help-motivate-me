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
    bool IsRepeatable,
    RepeatScheduleResponse? RepeatSchedule,
    int SortOrder,
    IEnumerable<TaskResponse> Subtasks,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RepeatScheduleResponse(
    RepeatFrequency Frequency,
    int IntervalValue,
    int[]? DaysOfWeek,
    int? DayOfMonth,
    DateOnly? NextOccurrence
);
