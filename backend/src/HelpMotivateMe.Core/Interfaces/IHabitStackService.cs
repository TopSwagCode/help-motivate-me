using HelpMotivateMe.Core.DTOs.HabitStacks;
namespace HelpMotivateMe.Core.Interfaces;

public interface IHabitStackService
{
    /// <summary>
    ///     Toggle completion for a habit stack item on a specific date.
    ///     Returns null if the item was not found.
    /// </summary>
    Task<HabitStackItemCompletionResult?> ToggleItemCompletionAsync(Guid itemId, Guid userId, DateOnly targetDate);

    /// <summary>
    ///     Complete all items in a habit stack for a specific date.
    ///     Returns null if the stack was not found.
    /// </summary>
    Task<CompleteAllResponse?> CompleteAllItemsAsync(Guid stackId, Guid userId, DateOnly targetDate);
}