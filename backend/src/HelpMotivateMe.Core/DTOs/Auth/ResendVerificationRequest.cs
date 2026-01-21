using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record ResendVerificationRequest(
    [Required] [EmailAddress] string Email
);
