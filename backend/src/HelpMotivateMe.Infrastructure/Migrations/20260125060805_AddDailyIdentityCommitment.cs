using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyIdentityCommitment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommitmentDefaultMode",
                table: "notification_preferences",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "weakest");

            migrationBuilder.AddColumn<bool>(
                name: "DailyCommitmentEnabled",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "daily_identity_commitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LinkedHabitStackItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Committed"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_identity_commitments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_identity_commitments_habit_stack_items_LinkedHabitSta~",
                        column: x => x.LinkedHabitStackItemId,
                        principalTable: "habit_stack_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_daily_identity_commitments_identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_identity_commitments_task_items_LinkedTaskId",
                        column: x => x.LinkedTaskId,
                        principalTable: "task_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_daily_identity_commitments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_CommitmentDate",
                table: "daily_identity_commitments",
                column: "CommitmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_IdentityId",
                table: "daily_identity_commitments",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_LinkedHabitStackItemId",
                table: "daily_identity_commitments",
                column: "LinkedHabitStackItemId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_LinkedTaskId",
                table: "daily_identity_commitments",
                column: "LinkedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_UserId",
                table: "daily_identity_commitments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_identity_commitments_UserId_CommitmentDate",
                table: "daily_identity_commitments",
                columns: new[] { "UserId", "CommitmentDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_identity_commitments");

            migrationBuilder.DropColumn(
                name: "CommitmentDefaultMode",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "DailyCommitmentEnabled",
                table: "notification_preferences");
        }
    }
}
