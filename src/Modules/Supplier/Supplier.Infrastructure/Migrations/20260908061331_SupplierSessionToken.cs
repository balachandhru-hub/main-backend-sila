using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierSessionToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "session_token",
                schema: "supplier",
                table: "supplier_rfq",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "session_token",
                schema: "supplier",
                table: "supplier_rfq");
        }
    }
}
