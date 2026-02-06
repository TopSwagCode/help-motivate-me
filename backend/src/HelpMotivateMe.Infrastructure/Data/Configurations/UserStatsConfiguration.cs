using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class UserStatsConfiguration : IEntityTypeConfiguration<UserStats>
{
    public void Configure(EntityTypeBuilder<UserStats> builder)
    {
        builder.ToTable("user_stats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.LoginCount)
            .HasColumnName("login_count");

        builder.Property(x => x.TotalWins)
            .HasColumnName("total_wins");

        builder.Property(x => x.TotalHabitsCompleted)
            .HasColumnName("total_habits_completed");

        builder.Property(x => x.TotalTasksCompleted)
            .HasColumnName("total_tasks_completed");

        builder.Property(x => x.TotalIdentityProofs)
            .HasColumnName("total_identity_proofs");

        builder.Property(x => x.TotalJournalEntries)
            .HasColumnName("total_journal_entries");

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(x => x.PreviousLoginAt)
            .HasColumnName("previous_login_at");

        builder.Property(x => x.LastActivityAt)
            .HasColumnName("last_activity_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on UserId (one row per user)
        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
