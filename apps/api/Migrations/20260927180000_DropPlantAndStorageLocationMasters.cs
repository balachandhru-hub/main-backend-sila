using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260927180000_DropPlantAndStorageLocationMasters")]
public partial class DropPlantAndStorageLocationMasters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS inventory_locations DROP COLUMN IF EXISTS "PlantMasterId";
            ALTER TABLE IF EXISTS inventory_locations DROP COLUMN IF EXISTS "StorageLocationMasterId";
            ALTER TABLE IF EXISTS inventory_locations DROP COLUMN IF EXISTS "Plant";
            ALTER TABLE IF EXISTS inventory_locations DROP COLUMN IF EXISTS "StorageLocation";
            DROP TABLE IF EXISTS storage_location_masters;
            DROP TABLE IF EXISTS plant_masters;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
