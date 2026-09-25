using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.HabitStacks;

public record CreateHabitStackRequest(
    string Name,
    string? Description,
    Guid? IdentityId,
    string? TriggerCue,
    List<HabitStackItemRequest>? Items,
    HabitStackDays OddWeekDays = HabitStackDays.EveryDay,
    HabitStackDays EvenWeekDays = HabitStackDays.EveryDay
);

public record UpdateHabitStackRequest(
    string Name,
    string? Description,
    Guid? IdentityId,
    string? TriggerCue,
    bool IsActive,
    HabitStackDays OddWeekDays = HabitStackDays.EveryDay,
    HabitStackDays EvenWeekDays = HabitStackDays.EveryDay
);

public record HabitStackItemRequest(
    string CueDescription,
    string HabitDescription
);

public record HabitStackResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityColor,
    string? TriggerCue,
    bool IsActive,
    HabitStackDays OddWeekDays,
    HabitStackDays EvenWeekDays,
    IEnumerable<HabitStackItemResponse> Items,
    DateTime CreatedAt
);

public record HabitStackItemResponse(
    Guid Id,
    string CueDescription,
    string HabitDescription,
    int SortOrder
);

public record AddStackItemRequest(
    string CueDescription,
    string HabitDescription
);

public record UpdateStackItemRequest(
    string CueDescription,
    string HabitDescription
);

public record ReorderStackItemsRequest(
    List<Guid> ItemIds
);

public record ReorderHabitStacksRequest(
    List<Guid> StackIds
);

// DTOs for Today view
public record TodayHabitStackResponse(
    Guid Id,
    string Name,
    string? TriggerCue,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityColor,
    string? IdentityIcon,
    IEnumerable<TodayHabitStackItemResponse> Items,
    int CompletedCount,
    int TotalCount
);

public record TodayHabitStackItemResponse(
    Guid Id,
    string HabitDescription,
    bool IsCompletedToday
);

// Response DTOs for habit completion
public record HabitStackItemCompletionResponse(
    Guid ItemId,
    string HabitDescription,
    bool IsCompleted
);

/// <summary>
///     Extended result from the service layer, includes extra data for analytics tracking.
/// </summary>
public record HabitStackItemCompletionResult(
    Guid ItemId,
    Guid HabitStackId,
    string HabitDescription,
    bool IsCompleted,
    bool WasNewlyCompleted
)
{
    public HabitStackItemCompletionResponse ToResponse()
    {
        return new HabitStackItemCompletionResponse(
            ItemId, HabitDescription, IsCompleted
        );
    }
}

public record CompleteAllResponse(
    Guid StackId,
    int CompletedCount,
    int TotalCount
);