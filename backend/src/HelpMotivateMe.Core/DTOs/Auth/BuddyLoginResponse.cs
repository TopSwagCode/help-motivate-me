namespace HelpMotivateMe.Core.DTOs.Auth;

public record BuddyLoginResponse(
    UserResponse User,
    Guid InviterUserId,
    string InviterDisplayName
);