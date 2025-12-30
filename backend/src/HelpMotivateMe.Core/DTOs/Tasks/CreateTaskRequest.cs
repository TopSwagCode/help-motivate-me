using System.ComponentModel.DataAnnotations;
using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.Tasks;

public record CreateTaskRequest(
    [Required, StringLength(255)] string Title,
    string? Description,
    DateOnly? DueDate,
    bool IsRepeatable = false,
    RepeatScheduleRequest? RepeatSchedule = null,
    Guid? IdentityId = null
);

public record RepeatScheduleRequest(
    RepeatFrequency Frequency,
    int IntervalValue = 1,
    int[]? DaysOfWeek = null,
    int? DayOfMonth = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null
);
