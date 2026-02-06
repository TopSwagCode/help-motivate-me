using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class JournalImageConfiguration : IEntityTypeConfiguration<JournalImage>
{
    public void Configure(EntityTypeBuilder<JournalImage> builder)
    {
        builder.ToTable("journal_images");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.FileName).HasMaxLength(255).IsRequired();
        builder.Property(i => i.S3Key).HasMaxLength(512).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(i => i.SortOrder).HasDefaultValue(0);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()");

        // Index
        builder.HasIndex(i => i.JournalEntryId);

        // Relationship
        builder.HasOne(i => i.JournalEntry)
            .WithMany(j => j.Images)
            .HasForeignKey(i => i.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
