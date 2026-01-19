using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_analytics_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_created_at",
                table: "analytics_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_event_type",
                table: "analytics_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_session_id",
                table: "analytics_events",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_user_id",
                table: "analytics_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_user_id_created_at",
                table: "analytics_events",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_events");
        }
    }
}
