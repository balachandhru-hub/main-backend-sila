using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSelectionModeFromRFQAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selection_mode",
                schema: "buyersystem",
                table: "rfqaward");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selection_mode",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
