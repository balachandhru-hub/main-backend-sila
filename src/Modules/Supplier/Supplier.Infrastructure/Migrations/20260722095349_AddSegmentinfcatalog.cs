using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentinfcatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "commodity_code",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_code",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_name",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segment_code",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segment_name",
                schema: "supplier",
                table: "supplier_catalog",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commodity_code",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "family_code",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "family_name",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "segment_code",
                schema: "supplier",
                table: "supplier_catalog");

            migrationBuilder.DropColumn(
                name: "segment_name",
                schema: "supplier",
                table: "supplier_catalog");
        }
    }
}
