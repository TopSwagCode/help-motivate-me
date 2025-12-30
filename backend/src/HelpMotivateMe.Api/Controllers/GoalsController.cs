using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Categories;
using HelpMotivateMe.Core.DTOs.Goals;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GoalsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<GoalResponse>>> GetGoals([FromQuery] Guid? categoryId)
    {
        var userId = GetUserId();

        var query = _db.Goals
            .Include(g => g.Categories)
            .Include(g => g.Tasks)
            .Where(g => g.UserId == userId);

        if (categoryId.HasValue)
        {
            query = query.Where(g => g.Categories.Any(c => c.Id == categoryId.Value));
        }

        var goals = await query
            .OrderBy(g => g.SortOrder)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();

        return Ok(goals.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoalResponse>> GetGoal(Guid id)
    {
        var userId = GetUserId();

        var goal = await _db.Goals
            .Include(g => g.Categories)
            .Include(g => g.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(goal));
    }

    [HttpPost]
    public async Task<ActionResult<GoalResponse>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var userId = GetUserId();

        var goal = new Goal
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            TargetDate = request.TargetDate
        };

        if (request.CategoryIds?.Count > 0)
        {
            var categories = await _db.Categories
                .Where(c => c.UserId == userId && request.CategoryIds.Contains(c.Id))
                .ToListAsync();
            goal.Categories = categories;
        }

        // Set sort order to be at the top
        var maxSortOrder = await _db.Goals
            .Where(g => g.UserId == userId)
            .MaxAsync(g => (int?)g.SortOrder) ?? 0;
        goal.SortOrder = maxSortOrder + 1;

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, MapToResponse(goal));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GoalResponse>> UpdateGoal(Guid id, [FromBody] UpdateGoalRequest request)
    {
        var userId = GetUserId();

        var goal = await _db.Goals
            .Include(g => g.Categories)
            .Include(g => g.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound();
        }

        goal.Title = request.Title;
        goal.Description = request.Description;
        goal.TargetDate = request.TargetDate;
        goal.UpdatedAt = DateTime.UtcNow;

        if (request.CategoryIds != null)
        {
            goal.Categories.Clear();
            if (request.CategoryIds.Count > 0)
            {
                var categories = await _db.Categories
                    .Where(c => c.UserId == userId && request.CategoryIds.Contains(c.Id))
                    .ToListAsync();
                foreach (var category in categories)
                {
                    goal.Categories.Add(category);
                }
            }
        }

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(goal));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        var userId = GetUserId();

        var goal = await _db.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound();
        }

        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Toggle completion for a goal on a specific date (defaults to client's current date if not provided).
    /// </summary>
    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<GoalResponse>> CompleteGoal(Guid id, [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var goal = await _db.Goals
            .Include(g => g.Categories)
            .Include(g => g.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound();
        }

        goal.IsCompleted = !goal.IsCompleted;
        goal.CompletedAt = goal.IsCompleted ? DateTime.SpecifyKind(targetDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
        goal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(goal));
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderGoals([FromBody] List<Guid> goalIds)
    {
        var userId = GetUserId();

        var goals = await _db.Goals
            .Where(g => g.UserId == userId && goalIds.Contains(g.Id))
            .ToListAsync();

        for (var i = 0; i < goalIds.Count; i++)
        {
            var goal = goals.FirstOrDefault(g => g.Id == goalIds[i]);
            if (goal != null)
            {
                goal.SortOrder = i;
            }
        }

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private static GoalResponse MapToResponse(Goal goal)
    {
        return new GoalResponse(
            goal.Id,
            goal.Title,
            goal.Description,
            goal.TargetDate,
            goal.IsCompleted,
            goal.CompletedAt,
            goal.SortOrder,
            goal.Tasks.Count,
            goal.Tasks.Count(t => t.Status == TaskItemStatus.Completed),
            goal.Categories.Select(c => new CategoryResponse(c.Id, c.Name, c.Color, c.Icon)),
            goal.CreatedAt,
            goal.UpdatedAt
        );
    }
}
