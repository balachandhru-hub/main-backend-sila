using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926210000_MaterialUnitPriceChange")]
public partial class MaterialUnitPriceChange : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE material_valuations ADD COLUMN IF NOT EXISTS "Plant" character varying(40);
            ALTER TABLE material_valuations ADD COLUMN IF NOT EXISTS "PriceUom" character varying(40);
            ALTER TABLE material_valuations ADD COLUMN IF NOT EXISTS "EffectiveFrom" timestamp with time zone;
            ALTER TABLE material_valuations ADD COLUMN IF NOT EXISTS "EffectiveTo" timestamp with time zone;
            ALTER TABLE material_valuations ADD COLUMN IF NOT EXISTS "Source" character varying(40);
            DROP INDEX IF EXISTS IX_material_valuations_area;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_material_valuations_current
                ON material_valuations ("MaterialId", "ValuationArea") WHERE "EffectiveTo" IS NULL;

            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "Reason" character varying(1000);
            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "CurrentUnitPrice" numeric(18,4);
            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "ProposedUnitPrice" numeric(18,4);
            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "PriceUom" character varying(40);
            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "Currency" character varying(8);
            ALTER TABLE material_change_requests ADD COLUMN IF NOT EXISTS "EffectiveFrom" timestamp with time zone;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS IX_material_valuations_current;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_material_valuations_area ON material_valuations ("MaterialId", "ValuationArea");
            ALTER TABLE material_valuations DROP COLUMN IF EXISTS "Plant";
            ALTER TABLE material_valuations DROP COLUMN IF EXISTS "PriceUom";
            ALTER TABLE material_valuations DROP COLUMN IF EXISTS "EffectiveFrom";
            ALTER TABLE material_valuations DROP COLUMN IF EXISTS "EffectiveTo";
            ALTER TABLE material_valuations DROP COLUMN IF EXISTS "Source";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "Reason";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "CurrentUnitPrice";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "ProposedUnitPrice";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "PriceUom";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "Currency";
            ALTER TABLE material_change_requests DROP COLUMN IF EXISTS "EffectiveFrom";
            """);
    }
}
