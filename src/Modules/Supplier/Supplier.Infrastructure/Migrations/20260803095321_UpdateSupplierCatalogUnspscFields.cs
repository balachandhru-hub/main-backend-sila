using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierCatalogUnspscFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commodity_code",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.RenameColumn(
                name: "segment_name",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "segment_title");

            migrationBuilder.RenameColumn(
                name: "segment_code",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "family_title");

            migrationBuilder.RenameColumn(
                name: "family_name",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "commodity_title");

            migrationBuilder.RenameColumn(
                name: "family_code",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "class_title");

            migrationBuilder.AddColumn<long>(
                name: "class",
                schema: "supplier",
                table: "supplier_catalog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "commodity",
                schema: "supplier",
                table: "supplier_catalog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "family",
                schema: "supplier",
                table: "supplier_catalog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "segment",
                schema: "supplier",
                table: "supplier_catalog",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "class",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "commodity",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "family",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "segment",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.RenameColumn(
                name: "segment_title",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "segment_name");

            migrationBuilder.RenameColumn(
                name: "family_title",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "segment_code");

            migrationBuilder.RenameColumn(
                name: "commodity_title",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "family_name");

            migrationBuilder.RenameColumn(
                name: "class_title",
                schema: "supplier",
                table: "supplier_catalog",
                newName: "family_code");

            migrationBuilder.AddColumn<string>(
                name: "commodity_code",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
