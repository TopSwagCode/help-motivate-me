using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class JournalReactionConfiguration : IEntityTypeConfiguration<JournalReaction>
{
    public void Configure(EntityTypeBuilder<JournalReaction> builder)
    {
        builder.ToTable("journal_reactions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.Emoji).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(r => r.JournalEntryId);
        builder.HasIndex(r => r.UserId);
        // Composite index to ensure unique reaction per user per emoji per entry
        builder.HasIndex(r => new { r.JournalEntryId, r.UserId, r.Emoji }).IsUnique();

        // Relationships
        builder.HasOne(r => r.JournalEntry)
            .WithMany(j => j.Reactions)
            .HasForeignKey(r => r.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}