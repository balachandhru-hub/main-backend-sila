using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSkuStockDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "available_stock",
                schema: "supplier",
                table: "supplier_catalog",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                schema: "supplier",
                table: "supplier_catalog",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_stock",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "sku",
                schema: "supplier",
                table: "supplier_catalog");
        }
    }
}
