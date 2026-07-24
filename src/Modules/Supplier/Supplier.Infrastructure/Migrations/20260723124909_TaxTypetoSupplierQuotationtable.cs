using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaxTypetoSupplierQuotationtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation");

            migrationBuilder.DropColumn(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation");
        }
    }
}
