namespace HelpMotivateMe.Core.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
