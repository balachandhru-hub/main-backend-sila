using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductTypetoItemBuyerMastertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alternate_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "base_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "micro_unit",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_type",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sub_unit",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit_of_measure_mapping",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "valuation_class",
                schema: "buyersystem",
                table: "item_buyer_master",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alternate_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "base_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "micro_unit",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "order_unit_of_measure",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "product_type",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "sub_unit",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "unit_of_measure_mapping",
                schema: "buyersystem",
                table: "item_buyer_master");

            migrationBuilder.DropColumn(
                name: "valuation_class",
                schema: "buyersystem",
                table: "item_buyer_master");
        }
    }
}
