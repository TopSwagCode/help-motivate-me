using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record RequestLoginLinkRequest(
    [Required] [EmailAddress] string Email
);