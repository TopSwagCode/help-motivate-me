using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/habit-stacks")]
public class HabitStacksController : ControllerBase
{
    private const string SessionIdKey = "AnalyticsSessionId";
    private readonly AppDbContext _db;
    private readonly IAnalyticsService _analyticsService;

    public HabitStacksController(AppDbContext db, IAnalyticsService analyticsService)
    {
        _db = db;
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<HabitStackResponse>>> GetHabitStacks()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "HabitStacksPageLoaded");

        var stacks = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items.OrderBy(i => i.SortOrder))
            .Where(hs => hs.UserId == userId)
            .OrderBy(hs => hs.SortOrder)
            .ThenBy(hs => hs.Name)
            .ToListAsync();

        return Ok(stacks.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HabitStackResponse>> GetHabitStack(Guid id)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(stack));
    }

    [HttpPost]
    public async Task<ActionResult<HabitStackResponse>> CreateHabitStack([FromBody] CreateHabitStackRequest request)
    {
        var userId = GetUserId();

        // Get max sort order for user's habit stacks
        var maxSortOrder = await _db.HabitStacks
            .Where(hs => hs.UserId == userId)
            .MaxAsync(hs => (int?)hs.SortOrder) ?? -1;

        var stack = new HabitStack
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            IdentityId = request.IdentityId,
            TriggerCue = request.TriggerCue,
            SortOrder = maxSortOrder + 1
        };

        if (request.Items != null)
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                stack.Items.Add(new HabitStackItem
                {
                    CueDescription = item.CueDescription,
                    HabitDescription = item.HabitDescription,
                    SortOrder = i
                });
            }
        }

        _db.HabitStacks.Add(stack);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(stack).Reference(s => s.Identity).LoadAsync();

        return CreatedAtAction(nameof(GetHabitStack), new { id = stack.Id }, MapToResponse(stack));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HabitStackResponse>> UpdateHabitStack(Guid id, [FromBody] UpdateHabitStackRequest request)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

        stack.Name = request.Name;
        stack.Description = request.Description;
        stack.IdentityId = request.IdentityId;
        stack.TriggerCue = request.TriggerCue;
        stack.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        // Reload identity if changed
        if (stack.IdentityId.HasValue)
        {
            await _db.Entry(stack).Reference(s => s.Identity).LoadAsync();
        }

        return Ok(MapToResponse(stack));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHabitStack(Guid id)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

        _db.HabitStacks.Remove(stack);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderHabitStacks([FromBody] ReorderHabitStacksRequest request)
    {
        var userId = GetUserId();

        var stacks = await _db.HabitStacks
            .Where(hs => hs.UserId == userId && request.StackIds.Contains(hs.Id))
            .ToListAsync();

        for (var i = 0; i < request.StackIds.Count; i++)
        {
            var stack = stacks.FirstOrDefault(s => s.Id == request.StackIds[i]);
            if (stack != null)
            {
                stack.SortOrder = i;
            }
        }

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<HabitStackResponse>> AddStackItem(Guid id, [FromBody] AddStackItemRequest request)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items)
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

        var maxSortOrder = stack.Items.Any() ? stack.Items.Max(i => i.SortOrder) : -1;

        var item = new HabitStackItem
        {
            HabitStackId = id,
            CueDescription = request.CueDescription,
            HabitDescription = request.HabitDescription,
            SortOrder = maxSortOrder + 1
        };

        stack.Items.Add(item);
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(stack));
    }

    [HttpPut("{id:guid}/items/reorder")]
    public async Task<ActionResult<HabitStackResponse>> ReorderStackItems(Guid id, [FromBody] ReorderStackItemsRequest request)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items)
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

        for (var i = 0; i < request.ItemIds.Count; i++)
        {
            var item = stack.Items.FirstOrDefault(it => it.Id == request.ItemIds[i]);
            if (item != null)
            {
                item.SortOrder = i;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(stack));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> DeleteStackItem(Guid itemId)
    {
        var userId = GetUserId();

        var item = await _db.HabitStackItems
            .Include(i => i.HabitStack)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.HabitStack.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        _db.HabitStackItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Toggle completion for a habit stack item on a specific date.
    /// </summary>
    [HttpPatch("items/{itemId:guid}/complete")]
    public async Task<ActionResult<HabitStackItemCompletionResponse>> CompleteStackItem(
        Guid itemId,
        [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var item = await _db.HabitStackItems
            .Include(i => i.HabitStack)
            .Include(i => i.Completions)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.HabitStack.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        var existingCompletion = item.Completions.FirstOrDefault(c => c.CompletedDate == targetDate);

        var wasCompleted = existingCompletion != null;

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

        if (!wasCompleted)
        {
            var sessionId = GetSessionId();
            await _analyticsService.LogEventAsync(userId, sessionId, "HabitCompleted", new { habitStackId = item.HabitStackId, itemId });
        }

        return Ok(new HabitStackItemCompletionResponse(
            item.Id,
            item.HabitDescription,
            item.CurrentStreak,
            item.LongestStreak,
            existingCompletion == null // isCompleted - true if we just added completion
        ));
    }

    /// <summary>
    /// Complete all items in a habit stack for a specific date.
    /// </summary>
    [HttpPatch("{id:guid}/complete-all")]
    public async Task<ActionResult<CompleteAllResponse>> CompleteAllStackItems(
        Guid id,
        [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var stack = await _db.HabitStacks
            .Include(hs => hs.Items)
                .ThenInclude(i => i.Completions)
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null)
        {
            return NotFound();
        }

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

        return Ok(new CompleteAllResponse(
            stack.Id,
            completedCount,
            stack.Items.Count
        ));
    }

    private void UpdateStreak(HabitStackItem item, DateOnly completedDate)
    {
        var yesterday = completedDate.AddDays(-1);
        var hadCompletionYesterday = item.Completions.Any(c => c.CompletedDate == yesterday);

        if (hadCompletionYesterday || item.CurrentStreak == 0)
        {
            item.CurrentStreak++;
        }
        else
        {
            // Gap in streak, reset to 1
            item.CurrentStreak = 1;
        }

        if (item.CurrentStreak > item.LongestStreak)
        {
            item.LongestStreak = item.CurrentStreak;
        }
    }

    private void RecalculateStreak(HabitStackItem item, DateOnly removedDate)
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private Guid GetSessionId()
    {
        var sessionIdString = HttpContext.Session.GetString(SessionIdKey);
        if (sessionIdString != null && Guid.TryParse(sessionIdString, out var sessionId))
        {
            return sessionId;
        }

        var newSessionId = Guid.NewGuid();
        HttpContext.Session.SetString(SessionIdKey, newSessionId.ToString());
        return newSessionId;
    }

    private static HabitStackResponse MapToResponse(HabitStack stack)
    {
        return new HabitStackResponse(
            stack.Id,
            stack.Name,
            stack.Description,
            stack.IdentityId,
            stack.Identity?.Name,
            stack.Identity?.Color,
            stack.TriggerCue,
            stack.IsActive,
            stack.Items.OrderBy(i => i.SortOrder).Select(i => new HabitStackItemResponse(
                i.Id,
                i.CueDescription,
                i.HabitDescription,
                i.SortOrder,
                i.CurrentStreak,
                i.LongestStreak
            )),
            stack.CreatedAt
        );
    }
}

public record HabitStackItemCompletionResponse(
    Guid ItemId,
    string HabitDescription,
    int CurrentStreak,
    int LongestStreak,
    bool IsCompleted
);

public record CompleteAllResponse(
    Guid StackId,
    int CompletedCount,
    int TotalCount
);
