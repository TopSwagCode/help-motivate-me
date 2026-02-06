using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(g => g.Title).HasMaxLength(255).IsRequired();
        builder.Property(g => g.Description).HasColumnType("text");
        builder.Property(g => g.IsCompleted).HasDefaultValue(false);
        builder.Property(g => g.SortOrder).HasDefaultValue(0);
        builder.Property(g => g.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(g => g.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(g => g.UserId);

        builder.HasOne(g => g.User)
            .WithMany(u => u.Goals)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional identity relationship
        builder.HasOne(g => g.Identity)
            .WithMany(i => i.Goals)
            .HasForeignKey(g => g.IdentityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
