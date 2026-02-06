namespace HelpMotivateMe.Core.DTOs.Auth;

public record UserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    IEnumerable<string> LinkedProviders,
    bool HasPassword,
    string MembershipTier,
    bool HasCompletedOnboarding,
    string Role,
    string PreferredLanguage
);
