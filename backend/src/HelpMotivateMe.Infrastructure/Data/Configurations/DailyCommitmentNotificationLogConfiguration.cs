using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class DailyCommitmentNotificationLogConfiguration : IEntityTypeConfiguration<DailyCommitmentNotificationLog>
{
    public void Configure(EntityTypeBuilder<DailyCommitmentNotificationLog> builder)
    {
        builder.ToTable("daily_commitment_notification_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.TimeSlot)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.SentAtUtc).HasDefaultValueSql("NOW()");

        // Unique constraint: one notification per user per day per time slot
        builder.HasIndex(l => new { l.UserId, l.LocalDate, l.TimeSlot }).IsUnique();

        // Indexes for queries
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.LocalDate);

        // Relationships
        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}