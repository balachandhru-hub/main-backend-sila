using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260919170000_IntegrationConnectionTests")]
public partial class IntegrationConnectionTests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "integration_connection_tests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SystemKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ProcessType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Protocol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                ErrorMessageSafe = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ChecksJson = table.Column<string>(type: "text", nullable: true),
                HttpStatus = table.Column<int>(type: "integer", nullable: true),
                DurationMs = table.Column<int>(type: "integer", nullable: false),
                TestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_integration_connection_tests", x => x.Id);
            });

                migrationBuilder.CreateIndex(
            name: "IX_integration_connection_tests_OrganizationId_ExpiresAt",
            table: "integration_connection_tests",
            columns: new[] { "OrganizationId", "ExpiresAt" });

        migrationBuilder.AddColumn<string>(
            name: "ConnectionStatus",
            table: "api_integration_configurations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConnectionStatus", table: "api_integration_configurations");
        migrationBuilder.DropTable(name: "integration_connection_tests");
    }
}
