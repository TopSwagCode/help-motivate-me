using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhoneEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HabitRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GoalRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DailyDigestEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    StreakAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MotivationalQuotesEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    WeeklyReviewEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    BuddyUpdatesEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SelectedDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 127),
                    PreferredTimeSlot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Morning"),
                    CustomTimeStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CustomTimeEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    TimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    UtcOffsetMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_UserId",
                table: "notification_preferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_preferences");
        }
    }
}
