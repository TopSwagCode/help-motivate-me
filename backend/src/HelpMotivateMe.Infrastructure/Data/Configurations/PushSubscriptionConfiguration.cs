using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.P256dh)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Auth)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        // Index on endpoint to quickly find/remove subscriptions
        builder.HasIndex(x => x.Endpoint);

        // Index on UserId for efficient user lookups
        builder.HasIndex(x => x.UserId);

        // Unique constraint to prevent duplicate subscriptions
        builder.HasIndex(x => new { x.UserId, x.Endpoint })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.PushSubscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
