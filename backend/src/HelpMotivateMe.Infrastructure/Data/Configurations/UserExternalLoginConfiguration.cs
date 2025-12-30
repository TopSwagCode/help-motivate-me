using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Provider).HasMaxLength(50).IsRequired();
        builder.Property(u => u.ProviderKey).HasMaxLength(255).IsRequired();
        builder.Property(u => u.ProviderDisplayName).HasMaxLength(255);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(u => new { u.Provider, u.ProviderKey }).IsUnique();
        builder.HasIndex(u => u.UserId);

        builder.HasOne(u => u.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
