using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(255);
        builder.Property(u => u.DisplayName).HasMaxLength(100);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("NOW()");
        builder.Property(u => u.MembershipTier)
            .HasDefaultValue(MembershipTier.Free)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(u => u.HasCompletedOnboarding).HasDefaultValue(false);
        builder.Property(u => u.Role)
            .HasDefaultValue(UserRole.User)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(u => u.PreferredLanguage)
            .HasDefaultValue(Language.English)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
