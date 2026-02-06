using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class DailyIdentityCommitmentConfiguration : IEntityTypeConfiguration<DailyIdentityCommitment>
{
    public void Configure(EntityTypeBuilder<DailyIdentityCommitment> builder)
    {
        builder.ToTable("daily_identity_commitments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.ActionDescription).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DailyCommitmentStatus.Committed);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

        // Unique constraint: one commitment per user per day
        builder.HasIndex(c => new { c.UserId, c.CommitmentDate }).IsUnique();

        // Indexes for queries
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IdentityId);
        builder.HasIndex(c => c.CommitmentDate);

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(i => i.DailyCommitments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Identity)
            .WithMany(i => i.DailyCommitments)
            .HasForeignKey(c => c.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.LinkedHabitStackItem)
            .WithMany(i => i.DailyCommitments)
            .HasForeignKey(c => c.LinkedHabitStackItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.LinkedTask)
            .WithMany(i => i.DailyCommitments)
            .HasForeignKey(c => c.LinkedTaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}