using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPoCreateDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "process",
                schema: "supplier",
                table: "supplier_erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PO_CREATE");

            migrationBuilder.AddColumn<string>(
                name: "request_body",
                schema: "supplier",
                table: "supplier_erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "process",
                schema: "supplier",
                table: "supplier_erp_integration_configuration");

            migrationBuilder.DropColumn(
                name: "request_body",
                schema: "supplier",
                table: "supplier_erp_integration_configuration");
        }
    }
}
