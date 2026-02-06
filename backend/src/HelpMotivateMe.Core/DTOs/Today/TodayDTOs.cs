namespace HelpMotivateMe.Core.DTOs.Today;

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
