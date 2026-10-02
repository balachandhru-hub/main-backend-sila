using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260917123000_SupplierSearchIndexes")]
public partial class SupplierSearchIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_suppliers_OrganizationId_Trn"
                ON suppliers ("OrganizationId", "Trn");
            CREATE INDEX IF NOT EXISTS "IX_suppliers_OrganizationId_SearchName"
                ON suppliers ("OrganizationId", "SearchName");
            CREATE INDEX IF NOT EXISTS "IX_supplier_aliases_OrganizationId_NormalizedAlias"
                ON supplier_aliases ("OrganizationId", "NormalizedAlias");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_suppliers_OrganizationId_Trn";
            DROP INDEX IF EXISTS "IX_suppliers_OrganizationId_SearchName";
            DROP INDEX IF EXISTS "IX_supplier_aliases_OrganizationId_NormalizedAlias";
            """);
    }
}
