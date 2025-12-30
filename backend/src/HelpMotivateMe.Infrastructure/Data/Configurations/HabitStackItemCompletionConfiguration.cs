using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class HabitStackItemCompletionConfiguration : IEntityTypeConfiguration<HabitStackItemCompletion>
{
    public void Configure(EntityTypeBuilder<HabitStackItemCompletion> builder)
    {
        builder.ToTable("habit_stack_item_completions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CompletedAt).HasDefaultValueSql("NOW()");

        // Unique constraint: one completion per item per day
        builder.HasIndex(c => new { c.HabitStackItemId, c.CompletedDate }).IsUnique();
        builder.HasIndex(c => c.HabitStackItemId);

        builder.HasOne(c => c.HabitStackItem)
            .WithMany(hsi => hsi.Completions)
            .HasForeignKey(c => c.HabitStackItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
