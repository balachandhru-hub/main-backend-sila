using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PunchOutUrltoSupplierCatalogtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "supplier",
                table: "supplier_catalog",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "catalog_type",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_punch_out",
                schema: "supplier",
                table: "supplier_catalog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "punch_out_url",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "catalog_type",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "is_punch_out",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "punch_out_url",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "supplier",
                table: "supplier_catalog",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
