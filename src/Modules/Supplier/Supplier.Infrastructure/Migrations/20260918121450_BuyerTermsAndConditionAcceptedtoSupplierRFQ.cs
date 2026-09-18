using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BuyerTermsAndConditionAcceptedtoSupplierRFQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "buyer_terms_and_condition_accepted",
                schema: "supplier",
                table: "supplier_rfq",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "buyer_terms_and_condition_accepted",
                schema: "supplier",
                table: "supplier_rfq");
        }
    }
}
