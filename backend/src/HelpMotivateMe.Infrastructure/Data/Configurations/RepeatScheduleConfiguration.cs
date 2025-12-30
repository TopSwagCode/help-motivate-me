using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class RepeatScheduleConfiguration : IEntityTypeConfiguration<RepeatSchedule>
{
    public void Configure(EntityTypeBuilder<RepeatSchedule> builder)
    {
        builder.ToTable("repeat_schedules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(r => r.IntervalValue).HasDefaultValue(1);
    }
}
