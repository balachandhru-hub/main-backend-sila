using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260925180000_MaterialMasterGovernance")]
public partial class MaterialMasterGovernance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "MaterialType" character varying(40);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "CompanyCode" character varying(40);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ValuationArea" character varying(40);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ValuationClass" character varying(40);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "PriceControl" character varying(8);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "StandardPrice" numeric(18,4);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "MovingAveragePrice" numeric(18,4);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "AcquisitionSource" character varying(16) NOT NULL DEFAULT 'MANUAL';
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "GovernanceStatus" character varying(24) NOT NULL DEFAULT 'ACTIVE';
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "ImportedAt" timestamp with time zone;
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "LastSynchronizedAt" timestamp with time zone;
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "SourceLastChangedAt" timestamp with time zone;
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "PendingChangeRequestId" uuid;
            CREATE TABLE IF NOT EXISTS material_valuations (
                "Id" uuid PRIMARY KEY, "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE CASCADE,
                "CompanyCode" character varying(40), "ValuationArea" character varying(40) NOT NULL,
                "ValuationClass" character varying(40), "PriceControl" character varying(8),
                "StandardPrice" numeric(18,4), "MovingAveragePrice" numeric(18,4), "Currency" character varying(8),
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_material_valuations_area ON material_valuations ("MaterialId", "ValuationArea");
            CREATE TABLE IF NOT EXISTS material_change_requests (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "MaterialId" uuid REFERENCES materials("Id") ON DELETE SET NULL,
                "MaterialCode" character varying(100) NOT NULL, "Event" character varying(40) NOT NULL,
                "Source" character varying(16) NOT NULL, "ProposedJson" text NOT NULL, "Status" character varying(24) NOT NULL,
                "SubmittedByUserId" uuid, "SubmittedAt" timestamp with time zone NOT NULL, "CompletedAt" timestamp with time zone);
            CREATE TABLE IF NOT EXISTS material_approval_actions (
                "Id" uuid PRIMARY KEY, "ChangeRequestId" uuid NOT NULL REFERENCES material_change_requests("Id") ON DELETE CASCADE,
                "MaterialId" uuid, "Event" character varying(40) NOT NULL, "Level" integer NOT NULL, "RoleKey" character varying(80),
                "ApproverUserId" uuid, "Status" character varying(24) NOT NULL, "Comment" text,
                "SubmittedAt" timestamp with time zone NOT NULL, "ActionAt" timestamp with time zone);
            CREATE TABLE IF NOT EXISTS material_erp_sync_states (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "CompanyCode" character varying(40) NOT NULL,
                "IntegrationRouteId" uuid, "IntegrationConfigurationId" uuid, "LastStatus" character varying(40),
                "LastSyncAt" timestamp with time zone, "LastReadCount" integer NOT NULL DEFAULT 0,
                "LastNewCount" integer NOT NULL DEFAULT 0, "LastChangedCount" integer NOT NULL DEFAULT 0,
                "LastUnchangedCount" integer NOT NULL DEFAULT 0, "LastFailedCount" integer NOT NULL DEFAULT 0, "LastError" text);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_material_erp_sync_states_cc ON material_erp_sync_states ("OrganizationId", "CompanyCode");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS material_approval_actions;
            DROP TABLE IF EXISTS material_change_requests;
            DROP TABLE IF EXISTS material_valuations;
            DROP TABLE IF EXISTS material_erp_sync_states;
            """);
    }
}
