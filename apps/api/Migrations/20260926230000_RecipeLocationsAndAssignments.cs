using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926230000_RecipeLocationsAndAssignments")]
public partial class RecipeLocationsAndAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS recipe_locations (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Kind" character varying(16) NOT NULL,
                "Code" character varying(80) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(500),
                "Status" character varying(16) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_locations_org_kind_code
                ON recipe_locations ("OrganizationId", "Kind", "Code");

            CREATE TABLE IF NOT EXISTS recipe_location_assignments (
                "Id" uuid PRIMARY KEY,
                "RecipeId" uuid NOT NULL REFERENCES recipes("Id") ON DELETE CASCADE,
                "LocationId" uuid NOT NULL REFERENCES recipe_locations("Id") ON DELETE CASCADE,
                "CreatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_location_assignments_recipe_location
                ON recipe_location_assignments ("RecipeId", "LocationId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS recipe_location_assignments;
            DROP TABLE IF EXISTS recipe_locations;
            """);
    }
}
