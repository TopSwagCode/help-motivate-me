using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class IdentityProof
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid IdentityId { get; set; }
    public DateOnly ProofDate { get; set; }
    public string? Description { get; set; }
    public ProofIntensity Intensity { get; set; } = ProofIntensity.Easy;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Identity Identity { get; set; } = null!;
}
