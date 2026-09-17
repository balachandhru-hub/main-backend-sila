using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRFQAwardSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rfqaward_item_rfqid",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "delivery_charge",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "delivery_type",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "discount_type",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "line_number",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "quantity",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "quoted_amount",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "quoted_price",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "rfqid",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "sub_total",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "supplier_rfqid",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "supplier_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "tax",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "tax_type",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "awarded_items",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "awarded_suppliers",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "buyer_id",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "currency",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "delivery_charge",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "delivery_type",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "discount_type",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "rfqnumber",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "tax",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "tax_type",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "total_award_value",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_quotation_id",
                schema: "buyersystem",
                table: "rfqaward",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_item_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "rfqitem_id");

            migrationBuilder.AddForeignKey(
                name: "fk_rfqaward_item_rfqitem_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "rfqitem_id",
                principalSchema: "buyersystem",
                principalTable: "rfqitem",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_rfqaward_item_rfqitem_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropIndex(
                name: "ix_rfqaward_item_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item");

            migrationBuilder.DropColumn(
                name: "supplier_quotation_id",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_charge",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_type",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_amount",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_price",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rfqid",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "sub_total",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_rfqid",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_rfqitem_id",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                schema: "buyersystem",
                table: "rfqaward_item",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "awarded_items",
                schema: "buyersystem",
                table: "rfqaward",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "awarded_suppliers",
                schema: "buyersystem",
                table: "rfqaward",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_id",
                schema: "buyersystem",
                table: "rfqaward",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "currency",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_charge",
                schema: "buyersystem",
                table: "rfqaward",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_type",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "buyersystem",
                table: "rfqaward",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rfqnumber",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tax",
                schema: "buyersystem",
                table: "rfqaward",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_award_value",
                schema: "buyersystem",
                table: "rfqaward",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_item_rfqid",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "rfqid");
        }
    }
}
