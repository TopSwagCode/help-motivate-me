using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class IdentityConfiguration : IEntityTypeConfiguration<Identity>
{
    public void Configure(EntityTypeBuilder<Identity> builder)
    {
        builder.ToTable("identities");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.Name).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Description).HasColumnType("text");
        builder.Property(i => i.Color).HasMaxLength(20);
        builder.Property(i => i.Icon).HasMaxLength(50);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(i => i.UserId);

        builder.HasOne(i => i.User)
            .WithMany(u => u.Identities)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
