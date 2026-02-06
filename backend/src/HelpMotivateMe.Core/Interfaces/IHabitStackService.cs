using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;

namespace HelpMotivateMe.Core.Interfaces;

public interface IHabitStackService
{
    /// <summary>
    /// Toggle completion for a habit stack item on a specific date.
    /// Returns null if the item was not found.
    /// </summary>
    Task<HabitStackItemCompletionResult?> ToggleItemCompletionAsync(Guid itemId, Guid userId, DateOnly targetDate);

    /// <summary>
    /// Complete all items in a habit stack for a specific date.
    /// Returns null if the stack was not found.
    /// </summary>
    Task<CompleteAllResponse?> CompleteAllItemsAsync(Guid stackId, Guid userId, DateOnly targetDate);

    /// <summary>
    /// Update streak for a habit stack item after completion.
    /// </summary>
    void UpdateStreak(HabitStackItem item, DateOnly completedDate);

    /// <summary>
    /// Recalculate streak after removing a completion.
    /// </summary>
    void RecalculateStreak(HabitStackItem item, DateOnly removedDate);
}
