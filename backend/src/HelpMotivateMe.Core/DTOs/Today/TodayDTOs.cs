using HelpMotivateMe.Core.DTOs.DailyCommitment;
using HelpMotivateMe.Core.DTOs.HabitStacks;

namespace HelpMotivateMe.Core.DTOs.Today;

public record TodayViewResponse(
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback,
    List<IdentityProgressResponse> IdentityProgress,
    DailyCommitmentResponse? DailyCommitment,
    YesterdayCommitmentResponse YesterdayCommitment
);

public record IdentityProgressResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    string Status,
    string Trend,
    int AccountAgeDays,
    bool ShowNumericScore,
    int TodayVotes
);

public record TodayTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid GoalId,
    string GoalTitle,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    string? IdentityColor,
    DateOnly? DueDate,
    string Status
);

public record TodayIdentityFeedbackResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int TotalVotes,
    int HabitVotes,
    int StackBonusVotes,
    int TaskVotes,
    int ProofVotes,
    string ReinforcementMessage
);

public record DailyDigestResponse(
    DateOnly Date,
    List<DailyDigestIdentityResponse> Identities,
    int TotalYesterdayVotes
);

public record DailyDigestIdentityResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int YesterdayScore,
    int TodayScore,
    string YesterdayStatus,
    string TodayStatus,
    string Trend,
    int YesterdayVotes,
    int HabitVotes,
    int StackBonusVotes,
    int TaskVotes,
    int ProofVotes
);