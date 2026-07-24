using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryLocationinSupplierRFQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_location",
                schema: "supplier",
                table: "supplier_rfq",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delivery_location",
                schema: "supplier",
                table: "supplier_rfq");
        }
    }
}
