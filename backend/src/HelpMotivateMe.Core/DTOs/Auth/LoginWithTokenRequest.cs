using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Auth;

public record LoginWithTokenRequest(
    [Required] string Token
);
