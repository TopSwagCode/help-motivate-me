namespace HelpMotivateMe.Core.DTOs.Categories;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon
);
