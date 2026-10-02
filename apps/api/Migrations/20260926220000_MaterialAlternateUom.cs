using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260926220000_MaterialAlternateUom")]
public partial class MaterialAlternateUom : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ConvFactor" numeric(18,6);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ConvUnit" character varying(40);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ConvValue" numeric(18,6);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE materials DROP COLUMN IF EXISTS "ConvFactor";
            ALTER TABLE materials DROP COLUMN IF EXISTS "ConvUnit";
            ALTER TABLE materials DROP COLUMN IF EXISTS "ConvValue";
            """);
    }
}
