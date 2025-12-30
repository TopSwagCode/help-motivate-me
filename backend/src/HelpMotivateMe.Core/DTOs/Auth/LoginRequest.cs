using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);
