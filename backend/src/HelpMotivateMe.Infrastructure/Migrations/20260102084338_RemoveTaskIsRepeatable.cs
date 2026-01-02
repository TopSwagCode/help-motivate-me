using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskIsRepeatable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_items_repeat_schedules_RepeatScheduleId",
                table: "task_items");

            migrationBuilder.DropTable(
                name: "repeat_schedules");

            migrationBuilder.DropIndex(
                name: "IX_task_items_RepeatScheduleId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "RepeatScheduleId",
                table: "task_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RepeatScheduleId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "repeat_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    DaysOfWeek = table.Column<int[]>(type: "integer[]", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IntervalValue = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LastCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextOccurrence = table.Column<DateOnly>(type: "date", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repeat_schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_items_RepeatScheduleId",
                table: "task_items",
                column: "RepeatScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_repeat_schedules_RepeatScheduleId",
                table: "task_items",
                column: "RepeatScheduleId",
                principalTable: "repeat_schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
