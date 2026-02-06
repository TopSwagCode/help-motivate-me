using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record CompleteMultipleTasksRequest(
    [Required] List<Guid> TaskIds,
    DateOnly? Date = null // Client's current date for completion
);

public record CompleteMultipleTasksResponse(
    int CompletedCount,
    int TotalCount
);