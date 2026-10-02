using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260925200000_RecipeMasterFields")]
public partial class RecipeMasterFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "Family" character varying(120);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "ServingSize" numeric(18,6);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "ServingUom" character varying(40);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "PosCode" character varying(80);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "PosItem" character varying(250);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "PosItemMenuPrice" numeric(18,4);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "LastSaleDate" date;
            UPDATE recipes SET "ServingSize" = COALESCE("ServingSize", 1), "ServingUom" = COALESCE("ServingUom", 'EA') WHERE "ServingUom" IS NULL;
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "ErpMaterialId" character varying(100);
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "MaterialGroup" character varying(80);
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "IngredientCost" numeric(18,4);
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "PercentageOfTotalCost" numeric(9,4);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipes DROP COLUMN IF EXISTS "Family";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "ServingSize";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "ServingUom";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "PosCode";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "PosItem";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "PosItemMenuPrice";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "LastSaleDate";
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "ErpMaterialId";
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "MaterialGroup";
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "IngredientCost";
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "PercentageOfTotalCost";
            """);
    }
}
