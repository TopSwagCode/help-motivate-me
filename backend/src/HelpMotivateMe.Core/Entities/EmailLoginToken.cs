namespace HelpMotivateMe.Core.Entities;

public class EmailLoginToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public required string Email { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
    public bool IsUsed => UsedAt.HasValue;

    public User User { get; set; } = null!;
}
