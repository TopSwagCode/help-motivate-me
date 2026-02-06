namespace HelpMotivateMe.Core.DTOs.Journal;

public record LinkableHabitStackResponse(Guid Id, string Name);

public record LinkableTaskResponse(Guid Id, string Title, string GoalTitle);
