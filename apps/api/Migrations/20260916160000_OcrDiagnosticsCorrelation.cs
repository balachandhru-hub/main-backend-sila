using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916160000_OcrDiagnosticsCorrelation")]
public partial class OcrDiagnosticsCorrelation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE documents
            ADD COLUMN IF NOT EXISTS "OcrRequestId" character varying(100);
            ALTER TABLE document_extractions
            ADD COLUMN IF NOT EXISTS "OcrRequestId" character varying(100);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE document_extractions DROP COLUMN IF EXISTS "OcrRequestId";
            ALTER TABLE documents DROP COLUMN IF EXISTS "OcrRequestId";
            """);
    }
}