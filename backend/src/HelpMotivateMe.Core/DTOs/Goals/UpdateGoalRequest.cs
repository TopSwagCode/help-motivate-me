using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Goals;

public record UpdateGoalRequest(
    [Required, StringLength(255)] string Title,
    string? Description,
    DateOnly? TargetDate
);
