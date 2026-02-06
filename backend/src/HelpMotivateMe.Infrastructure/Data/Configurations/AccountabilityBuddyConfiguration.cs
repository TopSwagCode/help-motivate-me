using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class AccountabilityBuddyConfiguration : IEntityTypeConfiguration<AccountabilityBuddy>
{
    public void Configure(EntityTypeBuilder<AccountabilityBuddy> builder)
    {
        builder.ToTable("accountability_buddies");

        builder.HasKey(ab => ab.Id);
        builder.Property(ab => ab.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ab => ab.CreatedAt).HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(ab => ab.UserId);
        builder.HasIndex(ab => ab.BuddyUserId);
        builder.HasIndex(ab => new { ab.UserId, ab.BuddyUserId }).IsUnique();

        // Relationships
        builder.HasOne(ab => ab.User)
            .WithMany()
            .HasForeignKey(ab => ab.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ab => ab.BuddyUser)
            .WithMany()
            .HasForeignKey(ab => ab.BuddyUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}