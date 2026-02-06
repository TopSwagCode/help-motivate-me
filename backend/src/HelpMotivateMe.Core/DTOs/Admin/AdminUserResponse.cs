namespace HelpMotivateMe.Core.DTOs.Admin;

public record AdminUserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    string MembershipTier,
    string Role,
    DateTime CreatedAt,
    DateTime? LastActiveAt,
    int AiCallsCount,
    decimal AiTotalCostUsd
);
