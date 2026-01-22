using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiBudgetTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "actual_cost_usd",
                table: "ai_usage_logs",
                type: "numeric(10,6)",
                precision: 10,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actual_cost_usd",
                table: "ai_usage_logs");
        }
    }
}
