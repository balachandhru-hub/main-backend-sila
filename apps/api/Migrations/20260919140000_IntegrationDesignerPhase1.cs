using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260919140000_IntegrationDesignerPhase1")]
public partial class IntegrationDesignerPhase1 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_api_integration_configurations_OrganizationId_EntityCode_ProcessType",
            table: "api_integration_configurations");

        migrationBuilder.AlterColumn<string>(
            name: "ProcessType",
            table: "api_integration_configurations",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30);

        migrationBuilder.AddColumn<string>(name: "Description", table: "api_integration_configurations", type: "character varying(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SystemKind", table: "api_integration_configurations", type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "CUSTOM");
        migrationBuilder.AddColumn<string>(name: "ServicePath", table: "api_integration_configurations", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "EntitySet", table: "api_integration_configurations", type: "character varying(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>(name: "HttpMethod", table: "api_integration_configurations", type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "GET");
        migrationBuilder.AddColumn<string>(name: "EnvironmentCode", table: "api_integration_configurations", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<int>(name: "Priority", table: "api_integration_configurations", type: "integer", nullable: false, defaultValue: 100);
        migrationBuilder.AddColumn<string>(name: "CompanyCode", table: "api_integration_configurations", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Plant", table: "api_integration_configurations", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PropertyCode", table: "api_integration_configurations", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "DesignerJson", table: "api_integration_configurations", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ValidationFingerprint", table: "api_integration_configurations", type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ValidatedAt", table: "api_integration_configurations", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ValidatedBy", table: "api_integration_configurations", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ValidationStatus", table: "api_integration_configurations", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastSuccessfulTestAt", table: "api_integration_configurations", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            UPDATE api_integration_configurations
            SET "ValidationStatus" = CASE
                WHEN "Status" = 'ACTIVE' THEN 'ACTIVE'
                WHEN "Status" = 'TESTED' THEN 'TESTED'
                WHEN "Status" = 'TEST_FAILED' THEN 'ERROR'
                ELSE 'DRAFT'
            END
            WHERE "ValidationStatus" IS NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_api_integration_configurations_OrganizationId_EntityCode_ProcessType",
            table: "api_integration_configurations",
            columns: new[] { "OrganizationId", "EntityCode", "ProcessType" });
        migrationBuilder.CreateIndex(
            name: "IX_api_integration_configurations_OrganizationId_ProcessType_Status_Priority_EntityCode_CompanyCode_Plant",
            table: "api_integration_configurations",
            columns: new[] { "OrganizationId", "ProcessType", "Status", "Priority", "EntityCode", "CompanyCode", "Plant" });

        migrationBuilder.AddColumn<string>(name: "SourceKind", table: "api_field_mappings", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "FIELD");
        migrationBuilder.AddColumn<string>(name: "SourceStructure", table: "api_field_mappings", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsCollection", table: "api_field_mappings", type: "boolean", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("The integration designer migration is additive and is not rolled back automatically.");
    }
}
