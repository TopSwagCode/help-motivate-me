namespace HelpMotivateMe.Core.DTOs.Auth;

public record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    IEnumerable<string> LinkedProviders
);
