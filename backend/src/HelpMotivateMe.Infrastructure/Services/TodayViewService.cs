using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.DTOs.Today;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

/// <summary>
/// Service for building the "Today" view data including habit stacks, tasks, and identity feedback.
/// Used by both TodayController and AccountabilityBuddyController.
/// </summary>
public class TodayViewService : ITodayViewService
{
    private readonly IQueryInterface<HabitStack> _habitStacks;
    private readonly IQueryInterface<TaskItem> _tasks;
    private readonly IQueryInterface<Identity> _identities;

    public TodayViewService(
        IQueryInterface<HabitStack> habitStacks,
        IQueryInterface<TaskItem> tasks,
        IQueryInterface<Identity> identities)
    {
        _habitStacks = habitStacks;
        _tasks = tasks;
        _identities = identities;
    }

    /// <summary>
    /// Get habit stacks with completion status for a specific user and date.
    /// </summary>
    public async Task<List<TodayHabitStackResponse>> GetTodayHabitStacksAsync(Guid userId, DateOnly targetDate)
    {
        var stacks = await _habitStacks
            .Include(s => s.Identity)
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Completions.Where(c => c.CompletedDate == targetDate))
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return stacks.Select(s => new TodayHabitStackResponse(
            s.Id,
            s.Name,
            s.TriggerCue,
            s.IdentityId,
            s.Identity?.Name,
            s.Identity?.Color,
            s.Identity?.Icon,
            s.Items.Select(i => new TodayHabitStackItemResponse(
                i.Id,
                i.HabitDescription,
                i.Completions.Any(),
                i.CurrentStreak
            )),
            s.Items.Count(i => i.Completions.Any()),
            s.Items.Count
        )).ToList();
    }

    /// <summary>
    /// Get upcoming (pending) tasks for a specific user within the next week.
    /// </summary>
    public async Task<List<TodayTaskResponse>> GetUpcomingTasksAsync(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        var tasks = await _tasks
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status != TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow))
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();

        return tasks.Select(MapToTaskResponse).ToList();
    }

    /// <summary>
    /// Get tasks completed on a specific date.
    /// </summary>
    public async Task<List<TodayTaskResponse>> GetCompletedTasksAsync(Guid userId, DateOnly targetDate)
    {
        var weekFromNow = targetDate.AddDays(7);

        var tasks = await _tasks
            .Include(t => t.Goal)
            .Include(t => t.Identity)
            .Where(t => t.Goal.UserId == userId &&
                        !t.Goal.IsCompleted &&
                        t.Status == TaskItemStatus.Completed &&
                        (!t.DueDate.HasValue || t.DueDate <= weekFromNow) &&
                        t.CompletedAt == targetDate)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync();

        return tasks.Select(MapToTaskResponse).ToList();
    }

    /// <summary>
    /// Get identity feedback including vote counts for a specific date.
    /// </summary>
    public async Task<List<TodayIdentityFeedbackResponse>> GetIdentityFeedbackAsync(Guid userId, DateOnly targetDate)
    {
        var identities = await _identities
            .Include(i => i.HabitStacks)
                .ThenInclude(hs => hs.Items)
                    .ThenInclude(hsi => hsi.Completions.Where(c => c.CompletedDate == targetDate))
            .Include(i => i.Tasks.Where(t => t.Status == TaskItemStatus.Completed &&
                                             t.CompletedAt == targetDate))
            .Include(i => i.Proofs.Where(p => p.ProofDate == targetDate))
            .Where(i => i.UserId == userId)
            .ToListAsync();

        return identities.Select(i =>
        {
            // Count habit stack item completions - 1 vote each
            var habitVotes = i.HabitStacks
                .SelectMany(hs => hs.Items)
                .SelectMany(hsi => hsi.Completions)
                .Count();

            // Count fully completed stacks - 2 bonus votes per completed stack
            var stackBonusVotes = i.HabitStacks
                .Where(hs => hs.Items.Count > 0 && hs.Items.All(item => item.Completions.Any()))
                .Count() * 2;

            // Count task completions - 2 votes each
            var taskVotes = i.Tasks.Count * 2;

            // Sum proof intensities (Easy=1, Moderate=2, Hard=3)
            var proofVotes = i.Proofs.Sum(p => (int)p.Intensity);

            var totalVotes = habitVotes + stackBonusVotes + taskVotes + proofVotes;

            return new TodayIdentityFeedbackResponse(
                i.Id,
                i.Name,
                i.Color,
                i.Icon,
                totalVotes,
                habitVotes,
                stackBonusVotes,
                taskVotes,
                proofVotes,
                GenerateReinforcementMessage(i.Name, totalVotes)
            );
        })
        .Where(i => i.TotalVotes > 0)
        .OrderByDescending(i => i.TotalVotes)
        .ToList();
    }

    /// <summary>
    /// Generate a motivational reinforcement message based on identity name and completion count.
    /// </summary>
    public static string GenerateReinforcementMessage(string identityName, int completions) =>
        completions switch
        {
            1 => $"You showed up as {identityName} today!",
            2 => $"Two votes for {identityName}!",
            >= 3 => $"Amazing! {completions} votes for {identityName} today!",
            _ => $"Keep building {identityName}!"
        };

    private static TodayTaskResponse MapToTaskResponse(TaskItem t) =>
        new(
            t.Id,
            t.Title,
            t.Description,
            t.GoalId,
            t.Goal.Title,
            t.IdentityId,
            t.Identity?.Name,
            t.Identity?.Icon,
            t.Identity?.Color,
            t.DueDate,
            t.Status.ToString()
        );
}
