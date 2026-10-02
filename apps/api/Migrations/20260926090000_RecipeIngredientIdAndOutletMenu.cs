using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926090000_RecipeIngredientIdAndOutletMenu")]
public partial class RecipeIngredientIdAndOutletMenu : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipe_ingredients ADD COLUMN IF NOT EXISTS "RecipeIngredientId" character varying(40);
            CREATE TABLE IF NOT EXISTS outlet_menu_items (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "OutletMappingId" uuid NOT NULL REFERENCES pos_outlet_mappings("Id") ON DELETE CASCADE,
                "RecipeId" uuid NOT NULL REFERENCES recipes("Id") ON DELETE RESTRICT,
                "PosCode" character varying(80),
                "PosItem" character varying(250),
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_outlet_menu_items_outlet_recipe ON outlet_menu_items ("OutletMappingId", "RecipeId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS outlet_menu_items;
            ALTER TABLE recipe_ingredients DROP COLUMN IF EXISTS "RecipeIngredientId";
            """);
    }
}
