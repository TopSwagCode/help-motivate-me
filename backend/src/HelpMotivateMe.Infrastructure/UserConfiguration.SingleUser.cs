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
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(user => user.Username).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(100);
        builder.Property(user => user.IsActive).HasDefaultValue(true);
        builder.Property(user => user.HasCompletedOnboarding).HasDefaultValue(false);
        builder.Property(user => user.PreferredLanguage)
            .HasDefaultValue(Language.English)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
