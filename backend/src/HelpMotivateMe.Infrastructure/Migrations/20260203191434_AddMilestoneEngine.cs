using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "domain_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_domain_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    trigger_event = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rule_data = table.Column<string>(type: "jsonb", nullable: false),
                    animation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    animation_data = table.Column<string>(type: "jsonb", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_count = table.Column<int>(type: "integer", nullable: false),
                    total_wins = table.Column<int>(type: "integer", nullable: false),
                    total_habits_completed = table.Column<int>(type: "integer", nullable: false),
                    total_tasks_completed = table.Column<int>(type: "integer", nullable: false),
                    total_identity_proofs = table.Column<int>(type: "integer", nullable: false),
                    total_journal_entries = table.Column<int>(type: "integer", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    previous_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_milestones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    has_been_seen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_milestones", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_milestones_milestone_definitions_milestone_definition_~",
                        column: x => x.milestone_definition_id,
                        principalTable: "milestone_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_milestones_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_occurred_at",
                table: "domain_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_user_id",
                table: "domain_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_user_id_event_type_occurred_at",
                table: "domain_events",
                columns: new[] { "user_id", "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_definitions_code",
                table: "milestone_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_definitions_is_active",
                table: "milestone_definitions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_definitions_trigger_event",
                table: "milestone_definitions",
                column: "trigger_event");

            migrationBuilder.CreateIndex(
                name: "IX_user_milestones_milestone_definition_id",
                table: "user_milestones",
                column: "milestone_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_milestones_user_id",
                table: "user_milestones",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_milestones_user_id_has_been_seen",
                table: "user_milestones",
                columns: new[] { "user_id", "has_been_seen" });

            migrationBuilder.CreateIndex(
                name: "IX_user_milestones_user_id_milestone_definition_id",
                table: "user_milestones",
                columns: new[] { "user_id", "milestone_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_stats_user_id",
                table: "user_stats",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domain_events");

            migrationBuilder.DropTable(
                name: "user_milestones");

            migrationBuilder.DropTable(
                name: "user_stats");

            migrationBuilder.DropTable(
                name: "milestone_definitions");
        }
    }
}
