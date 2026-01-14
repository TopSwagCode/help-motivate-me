using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdentityId",
                table: "goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_goals_IdentityId",
                table: "goals",
                column: "IdentityId");

            migrationBuilder.AddForeignKey(
                name: "FK_goals_identities_IdentityId",
                table: "goals",
                column: "IdentityId",
                principalTable: "identities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goals_identities_IdentityId",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "IX_goals_IdentityId",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "IdentityId",
                table: "goals");
        }
    }
}
