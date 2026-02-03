namespace HelpMotivateMe.Core.DTOs.Milestones;

// Response DTOs
public record MilestoneDefinitionResponse(
    Guid Id,
    string Code,
    string TitleKey,
    string DescriptionKey,
    string Icon,
    string TriggerEvent,
    string RuleType,
    string RuleData,
    string AnimationType,
    string? AnimationData,
    int SortOrder,
    bool IsActive
);

public record UserMilestoneResponse(
    Guid Id,
    Guid MilestoneDefinitionId,
    string Code,
    string TitleKey,
    string DescriptionKey,
    string Icon,
    string AnimationType,
    string? AnimationData,
    DateTime AwardedAt,
    bool HasBeenSeen
);

public record UserStatsResponse(
    int LoginCount,
    int TotalWins,
    int TotalHabitsCompleted,
    int TotalTasksCompleted,
    int TotalIdentityProofs,
    int TotalJournalEntries,
    DateTime? LastLoginAt,
    DateTime? LastActivityAt
);

// Request DTOs
public record MarkSeenRequest(List<Guid> MilestoneIds);
