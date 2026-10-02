using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260919220000_IntegrationRoutes")]
public partial class IntegrationRoutes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "integration_routes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                ProcessType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                SystemKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ApiIntegrationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                AppliesToAllCompanyCodes = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_integration_routes", x => x.Id);
                table.ForeignKey(
                    name: "FK_integration_routes_api_integration_configurations_ApiIntegrationConfigurationId",
                    column: x => x.ApiIntegrationConfigurationId,
                    principalTable: "api_integration_configurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_integration_routes_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "integration_route_company_codes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IntegrationRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_integration_route_company_codes", x => x.Id);
                table.ForeignKey(
                    name: "FK_integration_route_company_codes_integration_routes_IntegrationRouteId",
                    column: x => x.IntegrationRouteId,
                    principalTable: "integration_routes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_integration_routes_OrganizationId_ProcessType_IsActive",
            table: "integration_routes",
            columns: new[] { "OrganizationId", "ProcessType", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_integration_routes_ApiIntegrationConfigurationId",
            table: "integration_routes",
            column: "ApiIntegrationConfigurationId");

        migrationBuilder.CreateIndex(
            name: "IX_integration_route_company_codes_IntegrationRouteId_CompanyCode",
            table: "integration_route_company_codes",
            columns: new[] { "IntegrationRouteId", "CompanyCode" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "integration_route_company_codes");
        migrationBuilder.DropTable(name: "integration_routes");
    }
}
