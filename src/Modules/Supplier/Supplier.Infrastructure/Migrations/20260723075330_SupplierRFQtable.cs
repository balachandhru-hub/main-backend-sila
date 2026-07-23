using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierRFQtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_rfq",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    add_lot_option = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_rfq", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfqsupplier_mapping",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqsupplier_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqsupplier_mapping_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_quotation",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    delivery_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    delivery_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_quotation", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_quotation_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_rfqitem",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    uom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    material_group = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cost_center = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_rfqitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_rfqitem_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_quotation_item",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_quotation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quoted_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_quotation_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_quotation_item_supplier_quotation_supplier_quotation_id",
                        column: x => x.supplier_quotation_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_supplier_quotation_item_supplier_rfqitem_supplier_rfqitem_id",
                        column: x => x.supplier_rfqitem_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfqitem",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_rfqsupplier_mapping_is_active",
                schema: "supplier",
                table: "rfqsupplier_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqsupplier_mapping_supplier_rfqid",
                schema: "supplier",
                table: "rfqsupplier_mapping",
                column: "supplier_rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_is_active",
                schema: "supplier",
                table: "supplier_quotation",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation",
                column: "supplier_rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_is_active",
                schema: "supplier",
                table: "supplier_quotation_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item",
                column: "supplier_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item",
                column: "supplier_rfqitem_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_rfq_is_active",
                schema: "supplier",
                table: "supplier_rfq",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_rfqitem_is_active",
                schema: "supplier",
                table: "supplier_rfqitem",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_rfqitem_supplier_rfqid",
                schema: "supplier",
                table: "supplier_rfqitem",
                column: "supplier_rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqsupplier_mapping",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_quotation_item",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_quotation",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_rfqitem",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_rfq",
                schema: "supplier");
        }
    }
}
