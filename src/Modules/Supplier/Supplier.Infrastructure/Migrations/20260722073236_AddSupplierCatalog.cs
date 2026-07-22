using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_catalog",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    catalog_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_catalog", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_catalog_supplier_business_profile_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_asset_mapping",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_asset_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_asset_mapping_supplier_catalog_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "supplier",
                        principalTable: "supplier_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_asset_mapping_catalog_id",
                schema: "supplier",
                table: "catalog_asset_mapping",
                column: "catalog_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_asset_mapping_is_active",
                schema: "supplier",
                table: "catalog_asset_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_catalog_is_active",
                schema: "supplier",
                table: "supplier_catalog",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_catalog_supplier_id",
                schema: "supplier",
                table: "supplier_catalog",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_asset_mapping",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_catalog",
                schema: "supplier");
        }
    }
}
