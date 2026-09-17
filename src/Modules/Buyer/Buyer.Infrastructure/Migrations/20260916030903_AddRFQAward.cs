using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRFQAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rfqaward",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    selection_mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_award_value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    awarded_items = table.Column<int>(type: "int", nullable: false),
                    awarded_suppliers = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    tax_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delivery_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    delivery_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqaward", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqaward_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqaward_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqaward_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    line_number = table.Column<int>(type: "int", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_quotation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_quotation_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    quotation_version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    quoted_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    quoted_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    tax_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delivery_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    delivery_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sub_total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqaward_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqaward_item_rfqaward_rfqaward_id",
                        column: x => x.rfqaward_id,
                        principalSchema: "buyersystem",
                        principalTable: "rfqaward",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_is_active",
                schema: "buyersystem",
                table: "rfqaward",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_rfqid",
                schema: "buyersystem",
                table: "rfqaward",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_item_is_active",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_item_rfqaward_id",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "rfqaward_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqaward_item_rfqid",
                schema: "buyersystem",
                table: "rfqaward_item",
                column: "rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqaward_item",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqaward",
                schema: "buyersystem");
        }
    }
}
