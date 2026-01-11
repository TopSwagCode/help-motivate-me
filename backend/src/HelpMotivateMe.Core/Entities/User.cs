using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public MembershipTier MembershipTier { get; set; } = MembershipTier.Free;
    public bool HasCompletedOnboarding { get; set; } = false;
    public UserRole Role { get; set; } = UserRole.User;
    public Language PreferredLanguage { get; set; } = Language.English;

    // Navigation properties
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Identity> Identities { get; set; } = [];
    public ICollection<HabitStack> HabitStacks { get; set; } = [];
}
