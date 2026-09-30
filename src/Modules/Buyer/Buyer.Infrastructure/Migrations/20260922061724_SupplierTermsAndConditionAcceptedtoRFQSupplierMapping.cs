using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierTermsAndConditionAcceptedtoRFQSupplierMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_terms_and_condition_accepted",
                schema: "buyersystem",
                table: "rfq");

            migrationBuilder.AddColumn<bool>(
                name: "supplier_terms_and_condition_accepted",
                schema: "buyersystem",
                table: "rfqsupplier_mapping",
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
                table: "rfqsupplier_mapping");

            migrationBuilder.AddColumn<bool>(
                name: "supplier_terms_and_condition_accepted",
                schema: "buyersystem",
                table: "rfq",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
