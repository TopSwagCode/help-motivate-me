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
