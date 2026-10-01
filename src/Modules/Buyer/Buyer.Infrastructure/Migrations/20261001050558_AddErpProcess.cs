using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErpProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "process",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "WISHLIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "process",
                schema: "buyersystem",
                table: "erp_integration_configuration");
        }
    }
}
