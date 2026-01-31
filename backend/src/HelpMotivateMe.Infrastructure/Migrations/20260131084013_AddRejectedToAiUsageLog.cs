using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectedToAiUsageLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "rejected",
                table: "ai_usage_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rejected",
                table: "ai_usage_logs");
        }
    }
}
