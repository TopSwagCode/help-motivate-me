using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.PolarSubscriptionId).HasMaxLength(255).IsRequired();
        builder.HasIndex(s => s.PolarSubscriptionId).IsUnique();

        builder.Property(s => s.PolarCustomerId).HasMaxLength(255).IsRequired();
        builder.Property(s => s.ProductId).HasMaxLength(255).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(50).IsRequired();
        builder.Property(s => s.BillingInterval).HasMaxLength(20).IsRequired();

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.Status });
    }
}
