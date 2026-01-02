using System.ComponentModel.DataAnnotations;
using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record CreateTaskRequest(
    [Required, StringLength(255)] string Title,
    string? Description,
    DateOnly? DueDate,
    Guid? IdentityId = null
);
