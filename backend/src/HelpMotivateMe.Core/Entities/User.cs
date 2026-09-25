using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public int CredentialVersion { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool HasCompletedOnboarding { get; set; } = false;
    public Language PreferredLanguage { get; set; } = Language.English;

    // Navigation properties
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Identity> Identities { get; set; } = [];
    public ICollection<HabitStack> HabitStacks { get; set; } = [];
    public ICollection<DailyIdentityCommitment> DailyCommitments { get; set; } = [];
    public NotificationPreferences? NotificationPreferences { get; set; }
}