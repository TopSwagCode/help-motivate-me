using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class RepeatSchedule
{
    public Guid Id { get; set; }
    public RepeatFrequency Frequency { get; set; }
    public int IntervalValue { get; set; } = 1;
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? NextOccurrence { get; set; }
    public DateTime? LastCompleted { get; set; }

    // Navigation property
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
