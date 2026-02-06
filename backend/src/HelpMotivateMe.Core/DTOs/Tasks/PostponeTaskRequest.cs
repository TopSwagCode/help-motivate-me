using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record PostponeTaskRequest(
    [Required] DateOnly NewDueDate
);
