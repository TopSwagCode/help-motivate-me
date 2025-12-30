using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Categories;

public record CreateCategoryRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(7)] string? Color,
    [StringLength(50)] string? Icon
);
