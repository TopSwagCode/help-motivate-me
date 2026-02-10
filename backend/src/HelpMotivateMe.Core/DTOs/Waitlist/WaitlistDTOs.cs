namespace HelpMotivateMe.Core.DTOs.Waitlist;

public record WaitlistSignupRequest(string Email, string Name);

public record WaitlistEntryResponse(
    Guid Id,
    string Email,
    string Name,
    DateTime CreatedAt
);

public record WhitelistEntryResponse(
    Guid Id,
    string Email,
    DateTime AddedAt,
    string? AddedByEmail,
    DateTime? InvitedAt
);

public record InviteUserRequest(string Email);

public record WaitlistSignupResponse(string Message, bool CanSignup = false);

public record WhitelistCheckResponse(bool CanSignup);

public record SignupSettingsResponse(bool AllowSignups);