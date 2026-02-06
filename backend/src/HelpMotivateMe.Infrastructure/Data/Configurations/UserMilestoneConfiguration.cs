using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class UserMilestoneConfiguration : IEntityTypeConfiguration<UserMilestone>
{
    public void Configure(EntityTypeBuilder<UserMilestone> builder)
    {
        builder.ToTable("user_milestones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.MilestoneDefinitionId)
            .HasColumnName("milestone_definition_id")
            .IsRequired();

        builder.Property(x => x.AwardedAt)
            .HasColumnName("awarded_at");

        builder.Property(x => x.HasBeenSeen)
            .HasColumnName("has_been_seen");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MilestoneDefinition)
            .WithMany(x => x.UserMilestones)
            .HasForeignKey(x => x.MilestoneDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: each user can only be awarded each milestone once
        builder.HasIndex(x => new { x.UserId, x.MilestoneDefinitionId })
            .IsUnique();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.HasBeenSeen });
    }
}