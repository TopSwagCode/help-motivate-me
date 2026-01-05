namespace HelpMotivateMe.Core.Entities;

public class AiUsageLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? AudioDurationSeconds { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
