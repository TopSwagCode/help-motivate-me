using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpMotivateMe.Infrastructure.Data.Configurations;

public class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(np => np.Id);
        builder.Property(np => np.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(np => np.UserId).IsUnique();

        // Master toggle
        builder.Property(np => np.NotificationsEnabled).HasDefaultValue(true);

        // Delivery channels
        builder.Property(np => np.EmailEnabled).HasDefaultValue(true);
        builder.Property(np => np.SmsEnabled).HasDefaultValue(false);
        builder.Property(np => np.PhoneEnabled).HasDefaultValue(false);

        // Notification types
        builder.Property(np => np.HabitRemindersEnabled).HasDefaultValue(true);
        builder.Property(np => np.GoalRemindersEnabled).HasDefaultValue(true);
        builder.Property(np => np.DailyDigestEnabled).HasDefaultValue(true);
        builder.Property(np => np.StreakAlertsEnabled).HasDefaultValue(true);
        builder.Property(np => np.MotivationalQuotesEnabled).HasDefaultValue(true);
        builder.Property(np => np.WeeklyReviewEnabled).HasDefaultValue(true);
        builder.Property(np => np.BuddyUpdatesEnabled).HasDefaultValue(true);

        // Daily Identity Commitment
        builder.Property(np => np.DailyCommitmentEnabled).HasDefaultValue(true);
        builder.Property(np => np.CommitmentDefaultMode).HasMaxLength(20).HasDefaultValue("weakest");

        // Schedule - Days (store as integer for bit flags)
        // HasSentinel tells EF: when value equals None (CLR default), use the DB default instead
        builder.Property(np => np.SelectedDays)
            .HasDefaultValue(NotificationDays.All)
            .HasConversion<int>()
            .HasSentinel(NotificationDays.None);

        // Schedule - Time slot
        builder.Property(np => np.PreferredTimeSlot)
            .HasDefaultValue(TimeSlot.Morning)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Custom time (nullable)
        builder.Property(np => np.CustomTimeStart);
        builder.Property(np => np.CustomTimeEnd);

        // Timezone
        builder.Property(np => np.TimezoneId).HasMaxLength(100).HasDefaultValue("UTC");
        builder.Property(np => np.UtcOffsetMinutes).HasDefaultValue(0);

        // Timestamps
        builder.Property(np => np.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(np => np.UpdatedAt).HasDefaultValueSql("NOW()");

        // Relationship
        builder.HasOne(np => np.User)
            .WithOne(u => u.NotificationPreferences)
            .HasForeignKey<NotificationPreferences>(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
