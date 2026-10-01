using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErpApiDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "api_name",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_body",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "api_name",
                schema: "buyersystem",
                table: "erp_integration_configuration");

            migrationBuilder.DropColumn(
                name: "request_body",
                schema: "buyersystem",
                table: "erp_integration_configuration");
        }
    }
}
