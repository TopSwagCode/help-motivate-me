using System.Security.Claims;
using HelpMotivateMe.Core.DTOs.Categories;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var userId = GetUserId();

        var categories = await _db.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid id)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var userId = GetUserId();

        // Check for duplicate name
        if (await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == request.Name))
        {
            return BadRequest(new { message = "A category with this name already exists" });
        }

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToResponse(category));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] CreateCategoryRequest request)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            return NotFound();
        }

        // Check for duplicate name (excluding current category)
        if (await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == request.Name && c.Id != id))
        {
            return BadRequest(new { message = "A category with this name already exists" });
        }

        category.Name = request.Name;
        category.Color = request.Color;
        category.Icon = request.Icon;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(category));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            return NotFound();
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Color,
            category.Icon
        );
    }
}
