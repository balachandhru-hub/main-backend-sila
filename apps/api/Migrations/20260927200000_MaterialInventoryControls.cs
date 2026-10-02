using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260927200000_MaterialInventoryControls")]
public partial class MaterialInventoryControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "InventoryItem" boolean NOT NULL DEFAULT false;
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "InventoryType" character varying(16) NOT NULL DEFAULT 'NON_STOCK';
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "BatchManaged" boolean NOT NULL DEFAULT false;
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "ExpiryManaged" boolean NOT NULL DEFAULT false;
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "ShelfLifeDays" integer;
            ALTER TABLE IF EXISTS materials
                ADD COLUMN IF NOT EXISTS "SerialManaged" boolean NOT NULL DEFAULT false;
            ALTER TABLE IF EXISTS internal_transfer_orders
                ADD COLUMN IF NOT EXISTS "OneTimeTransfer" boolean NOT NULL DEFAULT false;
            CREATE TABLE IF NOT EXISTS material_locations (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "MaterialId" uuid NOT NULL,
                "InventoryLocationId" uuid NOT NULL,
                "StockingStatus" character varying(16) NOT NULL,
                "StockingType" character varying(16) NOT NULL,
                "MinimumStock" numeric(18,4),
                "MaximumStock" numeric(18,4),
                "ReorderPoint" numeric(18,4),
                "SafetyStock" numeric(18,4),
                "ParLevel" numeric(18,4),
                "PreferredSourceLocationId" uuid,
                "ReplenishmentMethod" character varying(80),
                "EffectiveFrom" timestamp with time zone,
                "EffectiveTo" timestamp with time zone,
                "Active" boolean NOT NULL DEFAULT true,
                "CreatedBy" uuid,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedBy" uuid,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_material_locations" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_material_locations_MaterialId_InventoryLocationId"
                ON material_locations ("MaterialId", "InventoryLocationId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS material_locations;
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "InventoryItem";
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "InventoryType";
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "BatchManaged";
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "ExpiryManaged";
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "ShelfLifeDays";
            ALTER TABLE IF EXISTS materials DROP COLUMN IF EXISTS "SerialManaged";
            ALTER TABLE IF EXISTS internal_transfer_orders DROP COLUMN IF EXISTS "OneTimeTransfer";
            """);
    }
}
