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
            _db.HabitStackItemCompletions.Remove(existingCompletion);
            item.Completions.Remove(existingCompletion);
        }
        else
        {
            var completion = new HabitStackItemCompletion
            {
                HabitStackItemId = itemId,
                CompletedDate = targetDate,
                CompletedAt = DateTime.UtcNow
            };
            _db.HabitStackItemCompletions.Add(completion);
            item.Completions.Add(completion);
        }

        await _db.SaveChangesAsync();

        return new HabitStackItemCompletionResult(
            item.Id,
            item.HabitStackId,
            item.HabitDescription,
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
                var completion = new HabitStackItemCompletion
                {
                    HabitStackItemId = item.Id,
                    CompletedDate = targetDate,
                    CompletedAt = DateTime.UtcNow
                };
                _db.HabitStackItemCompletions.Add(completion);
                item.Completions.Add(completion);
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

}