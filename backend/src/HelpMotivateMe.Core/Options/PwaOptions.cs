using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.Options;

public sealed class PwaOptions
{
    public const string SectionName = "Pwa";

    [Required]
    [MaxLength(80)]
    public string Name { get; init; } = "Help Motivate Me";

    [Required]
    [MaxLength(30)]
    public string ShortName { get; init; } = "Motivate Me";

    [MaxLength(160)]
    public string Description { get; init; } = "Track your goals, build habits, and become who you want to be";

    public string ThemeColor { get; init; } = "#d4944c";
    public string BackgroundColor { get; init; } = "#faf7f2";
}