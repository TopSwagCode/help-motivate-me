using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public class HabitStackService : IHabitStackService
{
    private readonly AppDbContext _db;

    public HabitStackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HabitStackItemCompletionResult?> ToggleItemCompletionAsync(Guid itemId, Guid userId,
        DateOnly targetDate)
    {
        var item = await _db.HabitStackItems
            .Include(i => i.HabitStack)
            .Include(i => i.Completions)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.HabitStack.UserId == userId);

        if (item == null) return null;

        var existingCompletion = item.Completions.FirstOrDefault(c => c.CompletedDate == targetDate);
        var wasNewlyCompleted = existingCompletion == null;

        if (existingCompletion != null)
        {
            // Toggle off - remove completion
            _db.HabitStackItemCompletions.Remove(existingCompletion);
            item.Completions.Remove(existingCompletion);

            // Recalculate streak
            RecalculateStreak(item, targetDate);
        }
        else
        {
            // Complete for this date
            var completion = new HabitStackItemCompletion
            {
                HabitStackItemId = itemId,
                CompletedDate = targetDate,
                CompletedAt = DateTime.UtcNow
            };
            _db.HabitStackItemCompletions.Add(completion);
            item.Completions.Add(completion);
            item.LastCompletedDate = targetDate;

            // Update streak
            UpdateStreak(item, targetDate);
        }

        await _db.SaveChangesAsync();

        return new HabitStackItemCompletionResult(
            item.Id,
            item.HabitStackId,
            item.HabitDescription,
            item.CurrentStreak,
            item.LongestStreak,
            wasNewlyCompleted, // IsCompleted - true if this was a new completion
            wasNewlyCompleted
        );
    }

    public async Task<CompleteAllResponse?> CompleteAllItemsAsync(Guid stackId, Guid userId, DateOnly targetDate)
    {
        var stack = await _db.HabitStacks
            .Include(hs => hs.Items)
            .ThenInclude(i => i.Completions)
            .FirstOrDefaultAsync(hs => hs.Id == stackId && hs.UserId == userId);

        if (stack == null) return null;

        var completedCount = 0;

        foreach (var item in stack.Items)
        {
            var existingCompletion = item.Completions.FirstOrDefault(c => c.CompletedDate == targetDate);

            if (existingCompletion == null)
            {
                // Complete for this date
                var completion = new HabitStackItemCompletion
                {
                    HabitStackItemId = item.Id,
                    CompletedDate = targetDate,
                    CompletedAt = DateTime.UtcNow
                };
                _db.HabitStackItemCompletions.Add(completion);
                item.Completions.Add(completion);
                item.LastCompletedDate = targetDate;

                // Update streak
                UpdateStreak(item, targetDate);
                completedCount++;
            }
        }

        await _db.SaveChangesAsync();

        return new CompleteAllResponse(
            stack.Id,
            completedCount,
            stack.Items.Count
        );
    }

    public void UpdateStreak(HabitStackItem item, DateOnly completedDate)
    {
        var yesterday = completedDate.AddDays(-1);
        var hadCompletionYesterday = item.Completions.Any(c => c.CompletedDate == yesterday);

        if (hadCompletionYesterday || item.CurrentStreak == 0)
            item.CurrentStreak++;
        else
            // Gap in streak, reset to 1
            item.CurrentStreak = 1;

        if (item.CurrentStreak > item.LongestStreak) item.LongestStreak = item.CurrentStreak;
    }

    public void RecalculateStreak(HabitStackItem item, DateOnly removedDate)
    {
        // Recalculate streak from scratch based on remaining completions
        var sortedCompletions = item.Completions
            .Where(c => c.CompletedDate != removedDate)
            .OrderByDescending(c => c.CompletedDate)
            .ToList();

        if (!sortedCompletions.Any())
        {
            item.CurrentStreak = 0;
            item.LastCompletedDate = null;
            return;
        }

        item.LastCompletedDate = sortedCompletions.First().CompletedDate;

        // Calculate current streak from most recent completion
        var streak = 1;
        var currentDate = sortedCompletions[0].CompletedDate;

        for (var i = 1; i < sortedCompletions.Count; i++)
        {
            var expectedPrevious = currentDate.AddDays(-1);
            if (sortedCompletions[i].CompletedDate == expectedPrevious)
            {
                streak++;
                currentDate = sortedCompletions[i].CompletedDate;
            }
            else
            {
                break;
            }
        }

        item.CurrentStreak = streak;
    }
}