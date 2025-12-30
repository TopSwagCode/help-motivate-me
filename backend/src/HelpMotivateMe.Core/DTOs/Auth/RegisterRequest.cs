using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record RegisterRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? DisplayName
);
