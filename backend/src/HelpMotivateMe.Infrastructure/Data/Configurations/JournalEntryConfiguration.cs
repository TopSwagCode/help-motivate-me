using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(j => j.Title).HasMaxLength(255).IsRequired();
        builder.Property(j => j.Description).HasColumnType("text");

        builder.Property(j => j.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(j => j.UpdatedAt).HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => j.EntryDate);
        builder.HasIndex(j => j.HabitStackId);
        builder.HasIndex(j => j.TaskItemId);

        // Check constraint: only one link allowed (either HabitStack or Task, not both)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_JournalEntry_SingleLink",
            "\"HabitStackId\" IS NULL OR \"TaskItemId\" IS NULL"
        ));

        // Relationships
        builder.HasOne(j => j.User)
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Author)
            .WithMany()
            .HasForeignKey(j => j.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.HabitStack)
            .WithMany()
            .HasForeignKey(j => j.HabitStackId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.TaskItem)
            .WithMany()
            .HasForeignKey(j => j.TaskItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}