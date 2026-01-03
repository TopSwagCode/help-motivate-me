using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountabilityBuddyFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthorUserId",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accountability_buddies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountability_buddies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accountability_buddies_users_BuddyUserId",
                        column: x => x.BuddyUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accountability_buddies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buddy_invite_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InviterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buddy_invite_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buddy_invite_tokens_users_BuddyUserId",
                        column: x => x.BuddyUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_buddy_invite_tokens_users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_AuthorUserId",
                table: "journal_entries",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_BuddyUserId",
                table: "accountability_buddies",
                column: "BuddyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_UserId",
                table: "accountability_buddies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_UserId_BuddyUserId",
                table: "accountability_buddies",
                columns: new[] { "UserId", "BuddyUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_BuddyUserId",
                table: "buddy_invite_tokens",
                column: "BuddyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_InvitedEmail",
                table: "buddy_invite_tokens",
                column: "InvitedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_InviterUserId",
                table: "buddy_invite_tokens",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_Token",
                table: "buddy_invite_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_journal_entries_users_AuthorUserId",
                table: "journal_entries",
                column: "AuthorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_entries_users_AuthorUserId",
                table: "journal_entries");

            migrationBuilder.DropTable(
                name: "accountability_buddies");

            migrationBuilder.DropTable(
                name: "buddy_invite_tokens");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_AuthorUserId",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "AuthorUserId",
                table: "journal_entries");
        }
    }
}
