namespace HelpMotivateMe.Core.DTOs.HabitStacks;

public record CreateHabitStackRequest(
    string Name,
    string? Description,
    Guid? IdentityId,
    string? TriggerCue,
    List<HabitStackItemRequest>? Items
);

public record UpdateHabitStackRequest(
    string Name,
    string? Description,
    Guid? IdentityId,
    string? TriggerCue,
    bool IsActive
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
    IEnumerable<HabitStackItemResponse> Items,
    DateTime CreatedAt
);

public record HabitStackItemResponse(
    Guid Id,
    string CueDescription,
    string HabitDescription,
    int SortOrder,
    int CurrentStreak,
    int LongestStreak
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
    bool IsCompletedToday,
    int CurrentStreak
);

// Response DTOs for habit completion
public record HabitStackItemCompletionResponse(
    Guid ItemId,
    string HabitDescription,
    int CurrentStreak,
    int LongestStreak,
    bool IsCompleted
);

/// <summary>
///     Extended result from the service layer, includes extra data for analytics tracking.
/// </summary>
public record HabitStackItemCompletionResult(
    Guid ItemId,
    Guid HabitStackId,
    string HabitDescription,
    int CurrentStreak,
    int LongestStreak,
    bool IsCompleted,
    bool WasNewlyCompleted
)
{
    public HabitStackItemCompletionResponse ToResponse()
    {
        return new HabitStackItemCompletionResponse(
            ItemId, HabitDescription, CurrentStreak, LongestStreak, IsCompleted
        );
    }
}

public record CompleteAllResponse(
    Guid StackId,
    int CompletedCount,
    int TotalCount
);