using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916170000_InvoiceDocumentSaveIntegrity")]
public partial class InvoiceDocumentSaveIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE documents
            ADD COLUMN IF NOT EXISTS "ContentHash" character varying(64);
            CREATE INDEX IF NOT EXISTS "IX_documents_OrganizationId_ContentHash"
                ON documents ("OrganizationId", "ContentHash");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_documents_OrganizationId_ContentHash";
            ALTER TABLE documents DROP COLUMN IF EXISTS "ContentHash";
            """);
    }
}