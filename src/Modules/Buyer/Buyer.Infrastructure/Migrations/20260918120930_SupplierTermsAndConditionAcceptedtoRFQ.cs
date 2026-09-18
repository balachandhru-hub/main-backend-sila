using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierTermsAndConditionAcceptedtoRFQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "supplier_terms_and_condition_accepted",
                schema: "buyersystem",
                table: "rfq",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_terms_and_condition_accepted",
                schema: "buyersystem",
                table: "rfq");
        }
    }
}
