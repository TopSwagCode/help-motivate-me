using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.DTOs.Today;

namespace HelpMotivateMe.Core.Interfaces;

public interface ITodayViewService
{
    Task<List<TodayHabitStackResponse>> GetTodayHabitStacksAsync(Guid userId, DateOnly targetDate);
    Task<List<TodayTaskResponse>> GetUpcomingTasksAsync(Guid userId, DateOnly targetDate);
    Task<List<TodayTaskResponse>> GetCompletedTasksAsync(Guid userId, DateOnly targetDate);
    Task<List<TodayIdentityFeedbackResponse>> GetIdentityFeedbackAsync(Guid userId, DateOnly targetDate);
}