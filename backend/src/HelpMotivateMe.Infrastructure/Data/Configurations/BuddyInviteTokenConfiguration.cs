using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class BuddyInviteTokenConfiguration : IEntityTypeConfiguration<BuddyInviteToken>
{
    public void Configure(EntityTypeBuilder<BuddyInviteToken> builder)
    {
        builder.ToTable("buddy_invite_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();
        builder.Property(t => t.InvitedEmail).HasMaxLength(255).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(t => t.UsedAt);

        // Indexes
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.InviterUserId);
        builder.HasIndex(t => t.BuddyUserId);
        builder.HasIndex(t => t.InvitedEmail);

        // Relationships
        builder.HasOne(t => t.InviterUser)
            .WithMany()
            .HasForeignKey(t => t.InviterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.BuddyUser)
            .WithMany()
            .HasForeignKey(t => t.BuddyUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}