using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
