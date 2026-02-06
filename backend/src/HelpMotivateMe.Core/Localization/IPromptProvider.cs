namespace HelpMotivateMe.Core.Localization;

public interface IPromptProvider
{
    string IdentitySystemPrompt { get; }
    string HabitStackSystemPrompt { get; }
    string GoalsSystemPrompt { get; }
    string GeneralTaskCreationPrompt { get; }
}