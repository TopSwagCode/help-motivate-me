using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.Options;

public sealed class SingleUserOptions
{
    public const string SectionName = "SingleUser";

    [Required]
    [MinLength(1)]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string? DisplayName { get; init; }
}
