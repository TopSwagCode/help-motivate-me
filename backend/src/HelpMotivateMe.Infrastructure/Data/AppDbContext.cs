using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<HabitStack> HabitStacks => Set<HabitStack>();
    public DbSet<HabitStackItem> HabitStackItems => Set<HabitStackItem>();
    public DbSet<HabitStackItemCompletion> HabitStackItemCompletions => Set<HabitStackItemCompletion>();
    public DbSet<EmailLoginToken> EmailLoginTokens => Set<EmailLoginToken>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalImage> JournalImages => Set<JournalImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
