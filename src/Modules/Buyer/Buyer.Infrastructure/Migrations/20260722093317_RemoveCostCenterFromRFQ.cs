using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCostCenterFromRFQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_center",
                schema: "buyersystem",
                table: "rfq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_center",
                schema: "buyersystem",
                table: "rfq",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
