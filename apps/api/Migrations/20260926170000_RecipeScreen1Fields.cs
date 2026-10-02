using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926170000_RecipeScreen1Fields")]
public partial class RecipeScreen1Fields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipe_categories ADD COLUMN IF NOT EXISTS "Description" character varying(500);
            CREATE TABLE IF NOT EXISTS recipe_families (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Code" character varying(80) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(500),
                "Status" character varying(16) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_families_org_code ON recipe_families ("OrganizationId", "Code");
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "FamilyId" uuid REFERENCES recipe_families("Id") ON DELETE SET NULL;
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "TotalServingQty" numeric(18,6);
            ALTER TABLE recipes ADD COLUMN IF NOT EXISTS "TotalServingUom" character varying(40);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE recipes DROP COLUMN IF EXISTS "FamilyId";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "TotalServingQty";
            ALTER TABLE recipes DROP COLUMN IF EXISTS "TotalServingUom";
            DROP TABLE IF EXISTS recipe_families;
            ALTER TABLE recipe_categories DROP COLUMN IF EXISTS "Description";
            """);
    }
}
