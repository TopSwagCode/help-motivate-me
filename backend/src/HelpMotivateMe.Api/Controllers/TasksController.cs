using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Tasks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("api/goals/{goalId:guid}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> GetTasks(Guid goalId)
    {
        var userId = GetUserId();

        // Verify goal belongs to user
        var goalExists = await _db.Goals.AnyAsync(g => g.Id == goalId && g.UserId == userId);
        if (!goalExists)
        {
            return NotFound(new { message = "Goal not found" });
        }

        var tasks = await _db.TaskItems
            .Include(t => t.RepeatSchedule)
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.RepeatSchedule)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.Identity)
            .Where(t => t.GoalId == goalId && t.ParentTaskId == null)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks.Select(MapToResponse));
    }

    [HttpGet("api/tasks/{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id)
    {
        var userId = GetUserId();

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.RepeatSchedule)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.Identity)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(task));
    }

    [HttpPost("api/goals/{goalId:guid}/tasks")]
    public async Task<ActionResult<TaskResponse>> CreateTask(Guid goalId, [FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();

        // Verify goal belongs to user
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        if (goal == null)
        {
            return NotFound(new { message = "Goal not found" });
        }

        var task = new TaskItem
        {
            GoalId = goalId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            IsRepeatable = request.IsRepeatable,
            IdentityId = request.IdentityId
        };

        if (request.IsRepeatable && request.RepeatSchedule != null)
        {
            task.RepeatSchedule = new RepeatSchedule
            {
                Frequency = request.RepeatSchedule.Frequency,
                IntervalValue = request.RepeatSchedule.IntervalValue,
                DaysOfWeek = request.RepeatSchedule.DaysOfWeek,
                DayOfMonth = request.RepeatSchedule.DayOfMonth,
                StartDate = request.RepeatSchedule.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = request.RepeatSchedule.EndDate,
                NextOccurrence = request.RepeatSchedule.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow)
            };
        }

        // Set sort order
        var maxSortOrder = await _db.TaskItems
            .Where(t => t.GoalId == goalId && t.ParentTaskId == null)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync() ?? 0;
        task.SortOrder = maxSortOrder + 1;

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        // Load identity for response
        if (task.IdentityId.HasValue)
        {
            await _db.Entry(task).Reference(t => t.Identity).LoadAsync();
        }

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, MapToResponse(task));
    }

    [HttpPost("api/tasks/{id:guid}/subtasks")]
    public async Task<ActionResult<TaskResponse>> CreateSubtask(Guid id, [FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();

        var parentTask = await _db.TaskItems
            .Include(t => t.Goal)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (parentTask == null)
        {
            return NotFound(new { message = "Parent task not found" });
        }

        var subtask = new TaskItem
        {
            GoalId = parentTask.GoalId,
            ParentTaskId = id,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            IsRepeatable = request.IsRepeatable,
            IdentityId = request.IdentityId
        };

        if (request.IsRepeatable && request.RepeatSchedule != null)
        {
            subtask.RepeatSchedule = new RepeatSchedule
            {
                Frequency = request.RepeatSchedule.Frequency,
                IntervalValue = request.RepeatSchedule.IntervalValue,
                DaysOfWeek = request.RepeatSchedule.DaysOfWeek,
                DayOfMonth = request.RepeatSchedule.DayOfMonth,
                StartDate = request.RepeatSchedule.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = request.RepeatSchedule.EndDate,
                NextOccurrence = request.RepeatSchedule.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow)
            };
        }

        // Set sort order
        var maxSortOrder = await _db.TaskItems
            .Where(t => t.ParentTaskId == id)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync() ?? 0;
        subtask.SortOrder = maxSortOrder + 1;

        _db.TaskItems.Add(subtask);
        await _db.SaveChangesAsync();

        // Load identity for response
        if (subtask.IdentityId.HasValue)
        {
            await _db.Entry(subtask).Reference(t => t.Identity).LoadAsync();
        }

        return CreatedAtAction(nameof(GetTask), new { id = subtask.Id }, MapToResponse(subtask));
    }

    [HttpPut("api/tasks/{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var userId = GetUserId();

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.Identity)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.IsRepeatable = request.IsRepeatable;
        task.IdentityId = request.IdentityId;
        task.UpdatedAt = DateTime.UtcNow;

        if (request.IsRepeatable && request.RepeatSchedule != null)
        {
            if (task.RepeatSchedule == null)
            {
                task.RepeatSchedule = new RepeatSchedule();
            }

            task.RepeatSchedule.Frequency = request.RepeatSchedule.Frequency;
            task.RepeatSchedule.IntervalValue = request.RepeatSchedule.IntervalValue;
            task.RepeatSchedule.DaysOfWeek = request.RepeatSchedule.DaysOfWeek;
            task.RepeatSchedule.DayOfMonth = request.RepeatSchedule.DayOfMonth;
            task.RepeatSchedule.StartDate = request.RepeatSchedule.StartDate ?? task.RepeatSchedule.StartDate;
            task.RepeatSchedule.EndDate = request.RepeatSchedule.EndDate;
        }
        else if (!request.IsRepeatable && task.RepeatSchedule != null)
        {
            _db.RepeatSchedules.Remove(task.RepeatSchedule);
            task.RepeatSchedule = null;
        }

        await _db.SaveChangesAsync();

        // Reload identity if changed
        if (task.IdentityId.HasValue)
        {
            await _db.Entry(task).Reference(t => t.Identity).LoadAsync();
        }
        else
        {
            task.Identity = null;
        }

        return Ok(MapToResponse(task));
    }

    [HttpDelete("api/tasks/{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var userId = GetUserId();

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Toggle completion for a task on a specific date (defaults to client's current date if not provided).
    /// </summary>
    [HttpPatch("api/tasks/{id:guid}/complete")]
    public async Task<ActionResult<TaskResponse>> CompleteTask(Guid id, [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.RepeatSchedule)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.Identity)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        if (task.Status == TaskItemStatus.Completed)
        {
            // Un-complete
            task.Status = TaskItemStatus.Pending;
            task.CompletedAt = null;
        }
        else
        {
            // Complete
            task.Status = TaskItemStatus.Completed;
            task.CompletedAt = targetDate;

            // Handle repeatable tasks
            if (task.IsRepeatable && task.RepeatSchedule != null)
            {
                task.RepeatSchedule.LastCompleted = DateTime.UtcNow;
                task.RepeatSchedule.NextOccurrence = CalculateNextOccurrence(task.RepeatSchedule);

                // Reset task for next occurrence
                task.Status = TaskItemStatus.Pending;
                task.CompletedAt = null;
            }
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(task));
    }

    /// <summary>
    /// Complete multiple tasks at once on a specific date (defaults to client's current date if not provided).
    /// </summary>
    [HttpPost("api/tasks/complete-multiple")]
    public async Task<ActionResult<CompleteMultipleTasksResponse>> CompleteMultipleTasks([FromBody] CompleteMultipleTasksRequest request)
    {
        var userId = GetUserId();
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .Where(t => request.TaskIds.Contains(t.Id) && t.Goal.UserId == userId)
            .ToListAsync();

        var completedCount = 0;

        foreach (var task in tasks)
        {
            if (task.Status != TaskItemStatus.Completed)
            {
                task.Status = TaskItemStatus.Completed;
                task.CompletedAt = targetDate;
                task.UpdatedAt = DateTime.UtcNow;

                // Handle repeatable tasks
                if (task.IsRepeatable && task.RepeatSchedule != null)
                {
                    task.RepeatSchedule.LastCompleted = DateTime.UtcNow;
                    task.RepeatSchedule.NextOccurrence = CalculateNextOccurrence(task.RepeatSchedule);
                    task.Status = TaskItemStatus.Pending;
                    task.CompletedAt = null;
                }

                completedCount++;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new CompleteMultipleTasksResponse(completedCount, tasks.Count));
    }

    [HttpPatch("api/tasks/{id:guid}/postpone")]
    public async Task<ActionResult<TaskResponse>> PostponeTask(Guid id, [FromBody] PostponeTaskRequest request)
    {
        var userId = GetUserId();

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.RepeatSchedule)
            .Include(t => t.Subtasks)
                .ThenInclude(s => s.Identity)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        task.DueDate = request.NewDueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(task));
    }

    [HttpPut("api/tasks/reorder")]
    public async Task<IActionResult> ReorderTasks([FromBody] List<Guid> taskIds)
    {
        var userId = GetUserId();

        var tasks = await _db.TaskItems
            .Include(t => t.Goal)
            .Where(t => taskIds.Contains(t.Id) && t.Goal.UserId == userId)
            .ToListAsync();

        for (var i = 0; i < taskIds.Count; i++)
        {
            var task = tasks.FirstOrDefault(t => t.Id == taskIds[i]);
            if (task != null)
            {
                task.SortOrder = i;
            }
        }

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("api/tasks/{id:guid}/tiny-version")]
    public async Task<ActionResult<TaskResponse>> CreateTinyVersion(Guid id)
    {
        var userId = GetUserId();

        var task = await _db.TaskItems
            .Include(t => t.Goal)
            .Include(t => t.RepeatSchedule)
            .FirstOrDefaultAsync(t => t.Id == id && t.Goal.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        // Check if tiny version already exists
        var existingTiny = await _db.TaskItems
            .AnyAsync(t => t.FullVersionTaskId == id);

        if (existingTiny)
        {
            return BadRequest(new { message = "A tiny version already exists for this task" });
        }

        // Get next sort order
        var maxSortOrder = await _db.TaskItems
            .Where(t => t.GoalId == task.GoalId && t.ParentTaskId == null)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync() ?? 0;

        var tinyTask = new TaskItem
        {
            GoalId = task.GoalId,
            Title = $"[2 min] {task.Title}",
            Description = "Tiny version: Start with just 2 minutes. The goal is to begin, not to finish.",
            IsTinyHabit = true,
            EstimatedMinutes = 2,
            FullVersionTaskId = task.Id,
            IsRepeatable = task.IsRepeatable,
            IdentityId = task.IdentityId,
            SortOrder = maxSortOrder + 1
        };

        // Copy repeat schedule if exists
        if (task.IsRepeatable && task.RepeatSchedule != null)
        {
            tinyTask.RepeatSchedule = new RepeatSchedule
            {
                Frequency = task.RepeatSchedule.Frequency,
                IntervalValue = task.RepeatSchedule.IntervalValue,
                DaysOfWeek = task.RepeatSchedule.DaysOfWeek,
                DayOfMonth = task.RepeatSchedule.DayOfMonth,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = task.RepeatSchedule.EndDate,
                NextOccurrence = DateOnly.FromDateTime(DateTime.UtcNow)
            };
        }

        _db.TaskItems.Add(tinyTask);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTask), new { id = tinyTask.Id }, MapToResponse(tinyTask));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private static DateOnly CalculateNextOccurrence(RepeatSchedule schedule)
    {
        var current = schedule.NextOccurrence ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return schedule.Frequency switch
        {
            RepeatFrequency.Daily => current.AddDays(schedule.IntervalValue),
            RepeatFrequency.Weekly => current.AddDays(7 * schedule.IntervalValue),
            RepeatFrequency.Monthly => current.AddMonths(schedule.IntervalValue),
            _ => current.AddDays(1)
        };
    }

    private static TaskResponse MapToResponse(TaskItem task)
    {
        return new TaskResponse(
            task.Id,
            task.GoalId,
            task.ParentTaskId,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.CompletedAt,
            task.IsRepeatable,
            task.RepeatSchedule != null
                ? new RepeatScheduleResponse(
                    task.RepeatSchedule.Frequency,
                    task.RepeatSchedule.IntervalValue,
                    task.RepeatSchedule.DaysOfWeek,
                    task.RepeatSchedule.DayOfMonth,
                    task.RepeatSchedule.NextOccurrence)
                : null,
            task.SortOrder,
            task.Subtasks.OrderBy(s => s.SortOrder).Select(MapToResponse),
            task.IdentityId,
            task.Identity?.Name,
            task.Identity?.Icon,
            task.CreatedAt,
            task.UpdatedAt
        );
    }
}
