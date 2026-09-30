using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierQuotationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_quotation_history",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_quotation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    delivery_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    delivery_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discount_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tax_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_quotation_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_quotation_history_supplier_quotation_supplier_quotation_id",
                        column: x => x.supplier_quotation_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_quotation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_supplier_quotation_history_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "supplier_quotation_item_history",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_quotation_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_quotation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quoted_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_quotation_item_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_quotation_item_history_supplier_quotation_item_supplier_quotation_item_id",
                        column: x => x.supplier_quotation_item_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_quotation_item",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_supplier_quotation_item_history_supplier_quotation_supplier_quotation_id",
                        column: x => x.supplier_quotation_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_quotation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_supplier_quotation_item_history_supplier_rfqitem_supplier_rfqitem_id",
                        column: x => x.supplier_rfqitem_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfqitem",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_history_is_active",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_history_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_history_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_is_active",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_supplier_quotation_item_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_rfqitem_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_quotation_history",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_quotation_item_history",
                schema: "supplier");
        }
    }
}
