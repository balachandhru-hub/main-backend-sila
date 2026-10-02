using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260917140000_OrganizationCustomerLogo")]
public partial class OrganizationCustomerLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "organizations"
                ADD COLUMN IF NOT EXISTS "CustomerLogoFileName" character varying(260),
                ADD COLUMN IF NOT EXISTS "CustomerLogoContentType" character varying(100),
                ADD COLUMN IF NOT EXISTS "CustomerLogoUpdatedAt" timestamp with time zone;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "organizations"
                DROP COLUMN IF EXISTS "CustomerLogoFileName",
                DROP COLUMN IF EXISTS "CustomerLogoContentType",
                DROP COLUMN IF EXISTS "CustomerLogoUpdatedAt";
            """);
    }
}
