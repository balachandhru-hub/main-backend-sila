using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260917210000_SupportSessionContext")]
public partial class SupportSessionContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sessions
                ADD COLUMN IF NOT EXISTS "IsSupportSession" boolean NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS "SupportSessionId" uuid,
                ADD COLUMN IF NOT EXISTS "PlatformUserId" uuid,
                ADD COLUMN IF NOT EXISTS "PlatformUserDisplayName" character varying(160),
                ADD COLUMN IF NOT EXISTS "TenantId" uuid,
                ADD COLUMN IF NOT EXISTS "TenantEnvironmentId" uuid;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sessions
                DROP COLUMN IF EXISTS "IsSupportSession",
                DROP COLUMN IF EXISTS "SupportSessionId",
                DROP COLUMN IF EXISTS "PlatformUserId",
                DROP COLUMN IF EXISTS "PlatformUserDisplayName",
                DROP COLUMN IF EXISTS "TenantId",
                DROP COLUMN IF EXISTS "TenantEnvironmentId";
            """);
    }
}
