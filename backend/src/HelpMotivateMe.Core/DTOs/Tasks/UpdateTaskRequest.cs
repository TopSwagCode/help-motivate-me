using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record UpdateTaskRequest(
    [Required] [StringLength(255)] string Title,
    string? Description,
    DateOnly? DueDate,
    Guid? IdentityId = null
);