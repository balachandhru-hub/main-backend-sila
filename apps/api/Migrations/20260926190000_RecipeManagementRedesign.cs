using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926190000_RecipeManagementRedesign")]
public partial class RecipeManagementRedesign : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS uom_masters (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Code" character varying(40) NOT NULL,
                "Name" character varying(80) NOT NULL,
                "Dimension" character varying(16) NOT NULL,
                "Status" character varying(16) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_uom_masters_org_code ON uom_masters ("OrganizationId", "Code");

            CREATE TABLE IF NOT EXISTS material_uom_conversions (
                "Id" uuid PRIMARY KEY,
                "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE CASCADE,
                "FromUom" character varying(40) NOT NULL,
                "ToUom" character varying(40) NOT NULL,
                "Numerator" numeric(18,6) NOT NULL,
                "Denominator" numeric(18,6) NOT NULL,
                "PackSize" numeric(18,6),
                "PackUom" character varying(40),
                "Source" character varying(40),
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_material_uom_conversions_unique
                ON material_uom_conversions ("MaterialId", "FromUom", "ToUom") WHERE "IsActive";
            CREATE INDEX IF NOT EXISTS IX_material_uom_conversions_material ON material_uom_conversions ("MaterialId");

            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "ItemMode" character varying(24) NOT NULL DEFAULT 'RECIPE';
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "YieldQty" numeric(18,6) NOT NULL DEFAULT 1;
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "YieldUom" character varying(40) NOT NULL DEFAULT 'EA';
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "Currency" character varying(8);

            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotMenuPrice" numeric(18,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotTotalRecipeCost" numeric(18,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotCostPerServing" numeric(18,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotCostPercent" numeric(9,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotMarginAmount" numeric(18,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotMarginPercent" numeric(9,4);
            ALTER TABLE recipe_versions ADD COLUMN IF NOT EXISTS "SnapshotCalculatedAt" timestamp with time zone;

            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "ConsumptionQuantity" numeric(18,6);
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "ConsumptionUom" character varying(40);

            CREATE INDEX IF NOT EXISTS IX_recipes_org_updated ON recipes ("OrganizationId", "UpdatedAt" DESC);
            CREATE INDEX IF NOT EXISTS IX_recipes_org_code ON recipes ("OrganizationId", "RecipeCode");
            CREATE INDEX IF NOT EXISTS IX_materials_org_code ON materials ("OrganizationId", "MaterialCode");
            CREATE INDEX IF NOT EXISTS IX_materials_org_normdesc ON materials ("OrganizationId", "NormalizedDescription");

            UPDATE recipes SET "YieldQty" = COALESCE("ServingSize", 1) WHERE "YieldQty" = 1 AND COALESCE("ServingSize", 1) <> 1;
            UPDATE recipes SET "YieldUom" = COALESCE(NULLIF("ServingUom", ''), "YieldUom");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "ConsumptionQuantity";
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "ConsumptionUom";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotMenuPrice";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotTotalRecipeCost";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotCostPerServing";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotCostPercent";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotMarginAmount";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotMarginPercent";
            ALTER TABLE recipe_versions DROP COLUMN IF EXISTS "SnapshotCalculatedAt";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "ItemMode";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "YieldQty";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "YieldUom";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "Currency";
            DROP TABLE IF EXISTS material_uom_conversions;
            DROP TABLE IF EXISTS uom_masters;
            """);
    }
}
