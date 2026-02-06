using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record VerifyEmailRequest(
    [Required] string Token
);