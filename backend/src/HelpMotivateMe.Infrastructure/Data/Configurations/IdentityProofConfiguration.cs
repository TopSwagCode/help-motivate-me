using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class IdentityProofConfiguration : IEntityTypeConfiguration<IdentityProof>
{
    public void Configure(EntityTypeBuilder<IdentityProof> builder)
    {
        builder.ToTable("identity_proofs");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Description).HasMaxLength(200);
        builder.Property(p => p.Intensity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ProofIntensity.Easy);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");

        // Indexes for queries
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.IdentityId);
        builder.HasIndex(p => p.ProofDate);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Identity)
            .WithMany(i => i.Proofs)
            .HasForeignKey(p => p.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
