using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260925190000_ApprovalWorkflowConfigurations")]
public partial class ApprovalWorkflowConfigurations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS approval_workflow_configurations (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Name" character varying(160) NOT NULL,
                "ApprovalType" character varying(40) NOT NULL,
                "Action" character varying(16) NOT NULL,
                "ScopeKind" character varying(24) NOT NULL,
                "ScopeValue" character varying(80),
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_approval_workflow_configurations_org ON approval_workflow_configurations ("OrganizationId");
            CREATE TABLE IF NOT EXISTS approval_workflow_levels (
                "Id" uuid PRIMARY KEY,
                "ConfigurationId" uuid NOT NULL REFERENCES approval_workflow_configurations("Id") ON DELETE CASCADE,
                "Level" integer NOT NULL,
                "RoleKey" character varying(80) NOT NULL,
                "Label" character varying(160) NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_approval_workflow_levels_level ON approval_workflow_levels ("ConfigurationId", "Level");
            CREATE TABLE IF NOT EXISTS approval_workflow_conditions (
                "Id" uuid PRIMARY KEY,
                "ConfigurationId" uuid NOT NULL REFERENCES approval_workflow_configurations("Id") ON DELETE CASCADE,
                "FieldKey" character varying(80) NOT NULL,
                "Operator" character varying(24) NOT NULL,
                "Value" numeric(18,4) NOT NULL);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS approval_workflow_conditions;
            DROP TABLE IF EXISTS approval_workflow_levels;
            DROP TABLE IF EXISTS approval_workflow_configurations;
            """);
    }
}
