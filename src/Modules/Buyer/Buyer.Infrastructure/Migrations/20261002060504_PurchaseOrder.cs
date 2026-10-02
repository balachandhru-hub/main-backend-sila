using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_order",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    po_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    weekly_bucket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    bucket_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    line_number = table.Column<int>(type: "int", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    discount_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    line_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    storage_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_item_purchase_order_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "buyersystem",
                        principalTable: "purchase_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_buyer_id",
                schema: "buyersystem",
                table: "purchase_order",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_is_active",
                schema: "buyersystem",
                table: "purchase_order",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_supplier_id",
                schema: "buyersystem",
                table: "purchase_order",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_weekly_bucket_id_supplier_id",
                schema: "buyersystem",
                table: "purchase_order",
                columns: new[] { "weekly_bucket_id", "supplier_id" },
                unique: true,
                filter: "[weekly_bucket_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_item_is_active",
                schema: "buyersystem",
                table: "purchase_order_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_item_purchase_order_id",
                schema: "buyersystem",
                table: "purchase_order_item",
                column: "purchase_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_order_item",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "purchase_order",
                schema: "buyersystem");
        }
    }
}
