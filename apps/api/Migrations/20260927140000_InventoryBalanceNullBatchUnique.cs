using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260927140000_InventoryBalanceNullBatchUnique")]
public partial class InventoryBalanceNullBatchUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM inventory_balances a
            USING inventory_balances b
            WHERE a."BatchId" IS NULL AND b."BatchId" IS NULL
              AND a."MaterialId" = b."MaterialId"
              AND a."InventoryLocationId" = b."InventoryLocationId"
              AND a."Id" > b."Id";
            CREATE UNIQUE INDEX IF NOT EXISTS IX_inventory_balances_material_location_nobatch
                ON inventory_balances ("MaterialId", "InventoryLocationId")
                WHERE "BatchId" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX IF EXISTS IX_inventory_balances_material_location_nobatch;""");
    }
}
