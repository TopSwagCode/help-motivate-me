using HelpMotivateMe.Core.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<HabitStack> HabitStacks => Set<HabitStack>();
    public DbSet<HabitStackItem> HabitStackItems => Set<HabitStackItem>();
    public DbSet<HabitStackItemCompletion> HabitStackItemCompletions => Set<HabitStackItemCompletion>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalImage> JournalImages => Set<JournalImage>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();
    public DbSet<DailyIdentityCommitment> DailyIdentityCommitments => Set<DailyIdentityCommitment>();
    public DbSet<IdentityProof> IdentityProofs => Set<IdentityProof>();
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();
    public DbSet<UserStats> UserStats => Set<UserStats>();
    public DbSet<MilestoneDefinition> MilestoneDefinitions => Set<MilestoneDefinition>();
    public DbSet<UserMilestone> UserMilestones => Set<UserMilestone>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
