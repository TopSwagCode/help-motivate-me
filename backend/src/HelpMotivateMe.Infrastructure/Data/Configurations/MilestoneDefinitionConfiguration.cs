using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class MilestoneDefinitionConfiguration : IEntityTypeConfiguration<MilestoneDefinition>
{
    public void Configure(EntityTypeBuilder<MilestoneDefinition> builder)
    {
        builder.ToTable("milestone_definitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TitleKey)
            .HasColumnName("title_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DescriptionKey)
            .HasColumnName("description_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Icon)
            .HasColumnName("icon")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TriggerEvent)
            .HasColumnName("trigger_event")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RuleType)
            .HasColumnName("rule_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RuleData)
            .HasColumnName("rule_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.AnimationType)
            .HasColumnName("animation_type")
            .HasMaxLength(50);

        builder.Property(x => x.AnimationData)
            .HasColumnName("animation_data")
            .HasColumnType("jsonb");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        // Unique constraint on Code
        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.TriggerEvent);
        builder.HasIndex(x => x.IsActive);
    }
}