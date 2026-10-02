using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260918190000_DelegatedSupportSession")]
public partial class DelegatedSupportSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sessions
                ALTER COLUMN "UserId" DROP NOT NULL,
                ADD COLUMN IF NOT EXISTS "PlatformUserEmail" character varying(320),
                ADD COLUMN IF NOT EXISTS "PlatformRole" character varying(80),
                ADD COLUMN IF NOT EXISTS "ActorType" character varying(40) NOT NULL DEFAULT 'CUSTOMER_USER',
                ADD COLUMN IF NOT EXISTS "SourcePlatformSessionId" uuid,
                ADD COLUMN IF NOT EXISTS "DelegatedPermissionsCsv" character varying(4000);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sessions
                DROP COLUMN IF EXISTS "PlatformUserEmail",
                DROP COLUMN IF EXISTS "PlatformRole",
                DROP COLUMN IF EXISTS "ActorType",
                DROP COLUMN IF EXISTS "SourcePlatformSessionId",
                DROP COLUMN IF EXISTS "DelegatedPermissionsCsv";
            """);
    }
}
