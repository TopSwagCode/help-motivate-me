using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class HabitStackItemConfiguration : IEntityTypeConfiguration<HabitStackItem>
{
    public void Configure(EntityTypeBuilder<HabitStackItem> builder)
    {
        builder.ToTable("habit_stack_items");

        builder.HasKey(hsi => hsi.Id);
        builder.Property(hsi => hsi.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(hsi => hsi.CueDescription).HasColumnType("text").IsRequired();
        builder.Property(hsi => hsi.HabitDescription).HasColumnType("text").IsRequired();
        builder.Property(hsi => hsi.SortOrder).HasDefaultValue(0);
        builder.Property(hsi => hsi.CurrentStreak).HasDefaultValue(0);
        builder.Property(hsi => hsi.LongestStreak).HasDefaultValue(0);
        builder.Property(hsi => hsi.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(hsi => hsi.HabitStackId);

        builder.HasOne(hsi => hsi.HabitStack)
            .WithMany(hs => hs.Items)
            .HasForeignKey(hsi => hsi.HabitStackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}