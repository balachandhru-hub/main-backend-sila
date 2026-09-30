using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaxtoSupplierQuotationItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "line_number",
                schema: "supplier",
                table: "supplier_rfqitem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_charge",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_type",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_amount",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sub_total",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_charge",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_type",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_amount",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sub_total",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation_item",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "line_number",
                schema: "supplier",
                table: "supplier_rfqitem");

            migrationBuilder.DropColumn(
                name: "delivery_charge",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "delivery_type",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "quoted_amount",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "sub_total",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "tax",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropColumn(
                name: "delivery_charge",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "delivery_type",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "discount_type",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "quoted_amount",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "sub_total",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "tax",
                schema: "supplier",
                table: "supplier_quotation_item");

            migrationBuilder.DropColumn(
                name: "tax_type",
                schema: "supplier",
                table: "supplier_quotation_item");
        }
    }
}
