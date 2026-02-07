using HelpMotivateMe.Core.DTOs.HabitStacks;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[Authorize]
[Route("api/habit-stacks")]
public class HabitStacksController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly AppDbContext _db;
    private readonly IHabitStackService _habitStackService;
    private readonly IQueryInterface<HabitStack> _habitStacksQuery;
    private readonly IMilestoneService _milestoneService;

    public HabitStacksController(
        AppDbContext db,
        IQueryInterface<HabitStack> habitStacksQuery,
        IAnalyticsService analyticsService,
        IMilestoneService milestoneService,
        IHabitStackService habitStackService)
    {
        _db = db;
        _habitStacksQuery = habitStacksQuery;
        _analyticsService = analyticsService;
        _milestoneService = milestoneService;
        _habitStackService = habitStackService;
    }

    [HttpGet]
    public async Task<ActionResult<List<HabitStackResponse>>> GetHabitStacks()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        await _analyticsService.LogEventAsync(userId, sessionId, "HabitStacksPageLoaded");

        var stacks = await _habitStacksQuery
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

        var stack = await _habitStacksQuery
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null) return NotFound();

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

        _db.HabitStacks.Add(stack);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(stack).Reference(s => s.Identity).LoadAsync();

        return CreatedAtAction(nameof(GetHabitStack), new { id = stack.Id }, MapToResponse(stack));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HabitStackResponse>> UpdateHabitStack(Guid id,
        [FromBody] UpdateHabitStackRequest request)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null) return NotFound();

        stack.Name = request.Name;
        stack.Description = request.Description;
        stack.IdentityId = request.IdentityId;
        stack.TriggerCue = request.TriggerCue;
        stack.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        // Reload identity if changed
        if (stack.IdentityId.HasValue) await _db.Entry(stack).Reference(s => s.Identity).LoadAsync();

        return Ok(MapToResponse(stack));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHabitStack(Guid id)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null) return NotFound();

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
            if (stack != null) stack.SortOrder = i;
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

        if (stack == null) return NotFound();

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
    public async Task<ActionResult<HabitStackResponse>> ReorderStackItems(Guid id,
        [FromBody] ReorderStackItemsRequest request)
    {
        var userId = GetUserId();

        var stack = await _db.HabitStacks
            .Include(hs => hs.Identity)
            .Include(hs => hs.Items)
            .FirstOrDefaultAsync(hs => hs.Id == id && hs.UserId == userId);

        if (stack == null) return NotFound();

        for (var i = 0; i < request.ItemIds.Count; i++)
        {
            var item = stack.Items.FirstOrDefault(it => it.Id == request.ItemIds[i]);
            if (item != null) item.SortOrder = i;
        }

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(stack));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateStackItem(Guid itemId, [FromBody] UpdateStackItemRequest request)
    {
        var userId = GetUserId();

        var item = await _db.HabitStackItems
            .Include(i => i.HabitStack)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.HabitStack.UserId == userId);

        if (item == null) return NotFound();

        item.CueDescription = request.CueDescription;
        item.HabitDescription = request.HabitDescription;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> DeleteStackItem(Guid itemId)
    {
        var userId = GetUserId();

        var item = await _db.HabitStackItems
            .Include(i => i.HabitStack)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.HabitStack.UserId == userId);

        if (item == null) return NotFound();

        _db.HabitStackItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    ///     Toggle completion for a habit stack item on a specific date.
    /// </summary>
    [HttpPatch("items/{itemId:guid}/complete")]
    public async Task<ActionResult<HabitStackItemCompletionResponse>> CompleteStackItem(
        Guid itemId,
        [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _habitStackService.ToggleItemCompletionAsync(itemId, userId, targetDate);

        if (result == null) return NotFound();

        // Log analytics if this was a new completion
        if (result.WasNewlyCompleted)
        {
            await _analyticsService.LogEventAsync(userId, sessionId, "HabitCompleted",
                new { habitStackId = result.HabitStackId, itemId });
            await _milestoneService.RecordEventAsync(userId, "HabitCompleted",
                new { habitStackId = result.HabitStackId, itemId });
        }

        return Ok(result.ToResponse());
    }

    /// <summary>
    ///     Complete all items in a habit stack for a specific date.
    /// </summary>
    [HttpPatch("{id:guid}/complete-all")]
    public async Task<ActionResult<CompleteAllResponse>> CompleteAllStackItems(
        Guid id,
        [FromQuery] DateOnly? date = null)
    {
        var userId = GetUserId();
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _habitStackService.CompleteAllItemsAsync(id, userId, targetDate);

        if (result == null) return NotFound();

        // Record milestone events for each completed habit
        for (var i = 0; i < result.CompletedCount; i++)
            await _milestoneService.RecordEventAsync(userId, "HabitCompleted", new { habitStackId = id });

        return Ok(result);
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