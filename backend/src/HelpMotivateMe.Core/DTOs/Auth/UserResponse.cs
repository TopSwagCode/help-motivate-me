namespace HelpMotivateMe.Core.DTOs.Auth;

public record UserResponse(
    Guid Id,
    string Username,
    string? DisplayName,
    DateTime CreatedAt,
    bool HasCompletedOnboarding,
    string PreferredLanguage
);