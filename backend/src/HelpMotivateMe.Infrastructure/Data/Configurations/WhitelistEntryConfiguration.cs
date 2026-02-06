using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class WhitelistEntryConfiguration : IEntityTypeConfiguration<WhitelistEntry>
{
    public void Configure(EntityTypeBuilder<WhitelistEntry> builder)
    {
        builder.ToTable("whitelist_entries");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(w => w.Email).HasMaxLength(255).IsRequired();
        builder.Property(w => w.AddedAt).HasDefaultValueSql("NOW()");
        builder.Property(w => w.InvitedAt);

        builder.HasIndex(w => w.Email).IsUnique();

        builder.HasOne(w => w.AddedByUser)
            .WithMany()
            .HasForeignKey(w => w.AddedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}