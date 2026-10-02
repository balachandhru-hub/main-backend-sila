using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260928120000_PosSalesUpdateStock")]
public partial class PosSalesUpdateStock : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS recipe_consumption_transactions
                ADD COLUMN IF NOT EXISTS "UploadBatchId" uuid;
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "SapPostingId" uuid;
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "HttpStatus" integer;
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "MaterialDocument" character varying(80);
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "DocumentYear" character varying(8);
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "ErrorCode" character varying(80);
            ALTER TABLE IF EXISTS recipe_inventory_postings
                ADD COLUMN IF NOT EXISTS "ErrorMessage" character varying(2000);
            CREATE TABLE IF NOT EXISTS pos_sales_upload_batches (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "PosSourceId" uuid NOT NULL,
                "FileName" character varying(260) NOT NULL,
                "UploadedByUserId" uuid,
                "UploadedAt" timestamp with time zone NOT NULL,
                "BusinessDateFrom" date,
                "BusinessDateTo" date,
                "Rows" integer NOT NULL,
                "Valid" integer NOT NULL,
                "Duplicates" integer NOT NULL,
                "Invalid" integer NOT NULL,
                "UnmappedPosCodes" integer NOT NULL,
                "InvalidOutlets" integer NOT NULL,
                "InvalidUom" integer NOT NULL,
                "RecipeNotReady" integer NOT NULL,
                "ReadyToProcess" integer NOT NULL,
                "Processed" integer NOT NULL,
                "Failed" integer NOT NULL,
                "PostingUnknown" integer NOT NULL,
                "Status" character varying(32) NOT NULL,
                CONSTRAINT "PK_pos_sales_upload_batches" PRIMARY KEY ("Id")
            );
            CREATE TABLE IF NOT EXISTS pos_sales_upload_lines (
                "Id" uuid NOT NULL,
                "BatchId" uuid NOT NULL,
                "RowNumber" integer NOT NULL,
                "BusinessDate" date,
                "TransactionId" character varying(120),
                "LineId" integer,
                "PosCode" character varying(80),
                "Qty" numeric(18,6),
                "Uom" character varying(40),
                "OutletId" character varying(80),
                "Currency" character varying(12),
                "Status" character varying(40) NOT NULL,
                "ErrorCode" character varying(80),
                "ErrorMessage" character varying(2000),
                "ConsumptionTransactionId" uuid,
                CONSTRAINT "PK_pos_sales_upload_lines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_pos_sales_upload_lines_batch" FOREIGN KEY ("BatchId") REFERENCES pos_sales_upload_batches ("Id") ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS recipe_sap_postings (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "UploadBatchId" uuid,
                "BusinessDate" date NOT NULL,
                "Plant" character varying(40) NOT NULL,
                "StorageLocation" character varying(40) NOT NULL,
                "CompanyCode" character varying(40),
                "Status" character varying(32) NOT NULL,
                "IntegrationRouteId" uuid,
                "IntegrationConfigurationId" uuid,
                "RequestJson" text,
                "ResponseJson" text,
                "HttpStatus" integer,
                "MaterialDocument" character varying(80),
                "DocumentYear" character varying(8),
                "ErrorCode" character varying(80),
                "ErrorMessage" character varying(2000),
                "CreatedAt" timestamp with time zone NOT NULL,
                "PostedAt" timestamp with time zone,
                CONSTRAINT "PK_recipe_sap_postings" PRIMARY KEY ("Id")
            );
            DROP INDEX IF EXISTS IX_recipe_consumption_identity;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_consumption_identity ON recipe_consumption_transactions
                ("OrganizationId", "PosSourceId", "BusinessDate", "SourceTransactionId", "SourceLineNumber");
            CREATE INDEX IF NOT EXISTS IX_pos_sales_upload_batches_org ON pos_sales_upload_batches ("OrganizationId", "UploadedAt");
            CREATE INDEX IF NOT EXISTS IX_pos_sales_upload_lines_batch ON pos_sales_upload_lines ("BatchId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS pos_sales_upload_lines;
            DROP TABLE IF EXISTS recipe_sap_postings;
            DROP TABLE IF EXISTS pos_sales_upload_batches;
            ALTER TABLE IF EXISTS recipe_consumption_transactions DROP COLUMN IF EXISTS "UploadBatchId";
            """);
    }
}
