using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierRFQItemAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "awarded_supplier_id",
                schema: "supplier",
                table: "supplier_rfqitem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_awarded",
                schema: "supplier",
                table: "supplier_rfqitem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "awarded_supplier_id",
                schema: "supplier",
                table: "supplier_rfqitem");

            migrationBuilder.DropColumn(
                name: "is_awarded",
                schema: "supplier",
                table: "supplier_rfqitem");
        }
    }
}
