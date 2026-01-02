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
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
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
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
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
            IdentityId = request.IdentityId
        };

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
            IdentityId = request.IdentityId
        };

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
        task.IdentityId = request.IdentityId;
        task.UpdatedAt = DateTime.UtcNow;

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
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
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
            .Include(t => t.Identity)
            .Include(t => t.Subtasks)
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
            IdentityId = task.IdentityId,
            SortOrder = maxSortOrder + 1
        };

        _db.TaskItems.Add(tinyTask);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTask), new { id = tinyTask.Id }, MapToResponse(tinyTask));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
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
