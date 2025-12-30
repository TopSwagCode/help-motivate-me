using HelpMotivateMe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("task_items");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Description).HasColumnType("text");
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Core.Enums.TaskItemStatus.Pending);
        builder.Property(t => t.IsRepeatable).HasDefaultValue(false);
        builder.Property(t => t.SortOrder).HasDefaultValue(0);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(t => t.GoalId);
        builder.HasIndex(t => t.ParentTaskId);

        builder.HasOne(t => t.Goal)
            .WithMany(g => g.Tasks)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.RepeatSchedule)
            .WithMany(r => r.Tasks)
            .HasForeignKey(t => t.RepeatScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Identity)
            .WithMany(i => i.Tasks)
            .HasForeignKey(t => t.IdentityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.IdentityId);

        // 2-Minute Rule: Self-referencing for tiny habit versions
        builder.Property(t => t.IsTinyHabit).HasDefaultValue(false);

        builder.HasOne(t => t.FullVersionTask)
            .WithMany(t => t.TinyVersions)
            .HasForeignKey(t => t.FullVersionTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.FullVersionTaskId);
    }
}
