using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> builder)
    {
        builder.ToTable("ai_usage_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Model)
            .HasColumnName("model")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InputTokens)
            .HasColumnName("input_tokens");

        builder.Property(x => x.OutputTokens)
            .HasColumnName("output_tokens");

        builder.Property(x => x.AudioDurationSeconds)
            .HasColumnName("audio_duration_seconds");

        builder.Property(x => x.EstimatedCostUsd)
            .HasColumnName("estimated_cost_usd")
            .HasPrecision(10, 6);

        builder.Property(x => x.ActualCostUsd)
            .HasColumnName("actual_cost_usd")
            .HasPrecision(10, 6);

        builder.Property(x => x.RequestType)
            .HasColumnName("request_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
