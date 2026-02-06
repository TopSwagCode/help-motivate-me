using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class HabitStackConfiguration : IEntityTypeConfiguration<HabitStack>
{
    public void Configure(EntityTypeBuilder<HabitStack> builder)
    {
        builder.ToTable("habit_stacks");

        builder.HasKey(hs => hs.Id);
        builder.Property(hs => hs.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(hs => hs.Name).HasMaxLength(100).IsRequired();
        builder.Property(hs => hs.Description).HasColumnType("text");
        builder.Property(hs => hs.TriggerCue).HasMaxLength(255);
        builder.Property(hs => hs.IsActive).HasDefaultValue(true);
        builder.Property(hs => hs.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(hs => hs.UserId);
        builder.HasIndex(hs => hs.IdentityId);

        builder.HasOne(hs => hs.User)
            .WithMany(u => u.HabitStacks)
            .HasForeignKey(hs => hs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(hs => hs.Identity)
            .WithMany(i => i.HabitStacks)
            .HasForeignKey(hs => hs.IdentityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
