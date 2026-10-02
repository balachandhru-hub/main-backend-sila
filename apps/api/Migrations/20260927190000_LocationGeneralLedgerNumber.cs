using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260927190000_LocationGeneralLedgerNumber")]
public partial class LocationGeneralLedgerNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS inventory_locations
                ADD COLUMN IF NOT EXISTS "GeneralLedgerNumber" character varying(40);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS inventory_locations
                DROP COLUMN IF EXISTS "GeneralLedgerNumber";
            """);
    }
}
