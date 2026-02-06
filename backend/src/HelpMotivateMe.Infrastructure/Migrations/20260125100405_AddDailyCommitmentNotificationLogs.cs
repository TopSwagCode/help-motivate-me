using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCommitmentNotificationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_commitment_notification_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_commitment_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_commitment_notification_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_commitment_notification_logs_LocalDate",
                table: "daily_commitment_notification_logs",
                column: "LocalDate");

            migrationBuilder.CreateIndex(
                name: "IX_daily_commitment_notification_logs_UserId",
                table: "daily_commitment_notification_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_commitment_notification_logs_UserId_LocalDate_TimeSlot",
                table: "daily_commitment_notification_logs",
                columns: new[] { "UserId", "LocalDate", "TimeSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_commitment_notification_logs");
        }
    }
}
