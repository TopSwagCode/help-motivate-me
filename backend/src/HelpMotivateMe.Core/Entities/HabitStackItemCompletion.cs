namespace HelpMotivateMe.Core.Entities;

public class HabitStackItemCompletion
{
    public Guid Id { get; set; }
    public Guid HabitStackItemId { get; set; }
    public HabitStackItem HabitStackItem { get; set; } = null!;
    public DateOnly CompletedDate { get; set; }
    public DateTime CompletedAt { get; set; }
}
