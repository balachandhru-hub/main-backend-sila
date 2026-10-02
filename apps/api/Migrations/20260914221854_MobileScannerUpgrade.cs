using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilaMe.Api.Migrations
{
    /// <inheritdoc />
    public partial class MobileScannerUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "documents"
                ADD COLUMN IF NOT EXISTS "ScanSessionId" character varying(100) NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_ScanSessionId",
                table: "documents",
                columns: new[] { "OrganizationId", "ScanSessionId" },
                unique: true,
                filter: "\"ScanSessionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documents_OrganizationId_ScanSessionId",
                table: "documents");

            migrationBuilder.Sql("""
                ALTER TABLE "documents" DROP COLUMN IF EXISTS "ScanSessionId";
                """);
        }
    }
}
