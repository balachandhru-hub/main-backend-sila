using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20261003160000_StockCount")]
public partial class StockCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS material_barcodes (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE RESTRICT,
                "Barcode" character varying(80) NOT NULL,
                "BarcodeType" character varying(24) NOT NULL,
                "PackUom" character varying(40),
                "PackQuantity" numeric(18,4),
                "Active" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_material_barcodes_org_code" ON material_barcodes ("OrganizationId", "Barcode");

            CREATE TABLE IF NOT EXISTS stock_count_sessions (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "CountNumber" character varying(40) NOT NULL,
                "CountType" character varying(16) NOT NULL,
                "PropertyId" uuid REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "InventoryLocationId" uuid NOT NULL REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "BusinessDate" timestamp with time zone NOT NULL,
                "BlindCount" boolean NOT NULL,
                "Status" character varying(32) NOT NULL,
                "CreatedBy" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "StartedBy" uuid,
                "StartedAt" timestamp with time zone,
                "SubmittedBy" uuid,
                "SubmittedAt" timestamp with time zone,
                "ReviewedBy" uuid,
                "ReviewedAt" timestamp with time zone,
                "CompletedAt" timestamp with time zone,
                "Notes" character varying(2000)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_count_sessions_number" ON stock_count_sessions ("OrganizationId", "CountNumber");

            CREATE TABLE IF NOT EXISTS stock_count_lines (
                "Id" uuid PRIMARY KEY,
                "StockCountSessionId" uuid NOT NULL REFERENCES stock_count_sessions("Id") ON DELETE CASCADE,
                "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE RESTRICT,
                "InventoryLocationId" uuid NOT NULL,
                "SystemQty" numeric(18,4) NOT NULL,
                "SystemUom" character varying(40) NOT NULL,
                "PhysicalQty" numeric(18,4),
                "PhysicalUom" character varying(40),
                "FullQty" numeric(18,4),
                "OpenQty" numeric(18,4),
                "OpenUom" character varying(40),
                "ConvertedPhysicalQty" numeric(18,4),
                "BaseUom" character varying(40),
                "VarianceQty" numeric(18,4),
                "VariancePercent" numeric(18,4),
                "UnitCost" numeric(18,4),
                "VarianceValue" numeric(18,4),
                "Currency" character varying(8),
                "CountMethod" character varying(16),
                "CountedBy" uuid,
                "CountedAt" timestamp with time zone,
                "Status" character varying(32) NOT NULL,
                "SapStatus" character varying(32) NOT NULL,
                "SapMaterialDocument" character varying(80),
                "SapError" character varying(2000),
                "InventoryTransactionId" uuid
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_count_lines_material" ON stock_count_lines ("StockCountSessionId", "MaterialId");

            CREATE TABLE IF NOT EXISTS stock_count_captures (
                "Id" uuid PRIMARY KEY,
                "StockCountLineId" uuid NOT NULL REFERENCES stock_count_lines("Id") ON DELETE CASCADE,
                "Sequence" integer NOT NULL,
                "FullQty" numeric(18,4),
                "FullUom" character varying(40),
                "OpenQty" numeric(18,4),
                "OpenUom" character varying(40),
                "ConvertedQty" numeric(18,4) NOT NULL,
                "BaseUom" character varying(40) NOT NULL,
                "Method" character varying(16) NOT NULL,
                "CountedBy" uuid NOT NULL,
                "CountedAt" timestamp with time zone NOT NULL,
                "Note" character varying(500)
            );

            CREATE TABLE IF NOT EXISTS stock_shortage_enquiries (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "EnquiryNumber" character varying(40) NOT NULL,
                "StockCountSessionId" uuid NOT NULL,
                "StockCountLineId" uuid NOT NULL REFERENCES stock_count_lines("Id") ON DELETE CASCADE,
                "MaterialId" uuid NOT NULL,
                "InventoryLocationId" uuid NOT NULL,
                "SystemQty" numeric(18,4) NOT NULL,
                "PhysicalQty" numeric(18,4) NOT NULL,
                "ShortageQty" numeric(18,4) NOT NULL,
                "Uom" character varying(40) NOT NULL,
                "UnitCost" numeric(18,4),
                "ShortageValue" numeric(18,4),
                "Currency" character varying(8),
                "AssignedManagerUserId" uuid,
                "AssignedManagerGroup" character varying(80),
                "ManagerConfigured" boolean NOT NULL,
                "Status" character varying(40) NOT NULL,
                "Category" character varying(40),
                "CreatedAt" timestamp with time zone NOT NULL,
                "RespondedAt" timestamp with time zone,
                "ReviewedAt" timestamp with time zone,
                "ClosedAt" timestamp with time zone
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_shortage_enquiries_number" ON stock_shortage_enquiries ("OrganizationId", "EnquiryNumber");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_shortage_enquiries_line" ON stock_shortage_enquiries ("StockCountLineId");

            CREATE TABLE IF NOT EXISTS stock_shortage_messages (
                "Id" uuid PRIMARY KEY,
                "EnquiryId" uuid NOT NULL REFERENCES stock_shortage_enquiries("Id") ON DELETE CASCADE,
                "Kind" character varying(32) NOT NULL,
                "Category" character varying(40),
                "Comments" character varying(4000) NOT NULL,
                "AttachmentName" character varying(260),
                "ActorUserId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS stock_shortage_messages;
            DROP TABLE IF EXISTS stock_shortage_enquiries;
            DROP TABLE IF EXISTS stock_count_captures;
            DROP TABLE IF EXISTS stock_count_lines;
            DROP TABLE IF EXISTS stock_count_sessions;
            DROP TABLE IF EXISTS material_barcodes;
            """);
    }
}
