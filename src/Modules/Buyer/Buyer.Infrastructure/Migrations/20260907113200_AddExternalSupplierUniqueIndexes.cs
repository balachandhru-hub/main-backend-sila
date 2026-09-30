using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalSupplierUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rfqexternal_supplier_rfqid",
                schema: "buyersystem",
                table: "rfqexternal_supplier");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "buyersystem",
                table: "external_supplier",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "ix_rfqexternal_supplier_rfqid_external_supplier_id",
                schema: "buyersystem",
                table: "rfqexternal_supplier",
                columns: new[] { "rfqid", "external_supplier_id" },
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_external_supplier_email",
                schema: "buyersystem",
                table: "external_supplier",
                column: "email",
                unique: true,
                filter: "[is_active] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rfqexternal_supplier_rfqid_external_supplier_id",
                schema: "buyersystem",
                table: "rfqexternal_supplier");

            migrationBuilder.DropIndex(
                name: "ix_external_supplier_email",
                schema: "buyersystem",
                table: "external_supplier");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "buyersystem",
                table: "external_supplier",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "ix_rfqexternal_supplier_rfqid",
                schema: "buyersystem",
                table: "rfqexternal_supplier",
                column: "rfqid");
        }
    }
}
