using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260925140000_RecipeManagementFoundation")]
public partial class RecipeManagementFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "MaterialGroup" character varying(150);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "AlternateUom" character varying(50);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "UnitCost" numeric(18,4);
            ALTER TABLE materials ADD COLUMN IF NOT EXISTS "Currency" character varying(8);
            """);
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS recipe_categories (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Code" character varying(80) NOT NULL, "Name" character varying(200) NOT NULL, "Status" character varying(16) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_categories_org_code ON recipe_categories ("OrganizationId", "Code");
            CREATE TABLE IF NOT EXISTS recipes (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "RecipeCode" character varying(80) NOT NULL, "Name" character varying(250) NOT NULL, "Description" text,
                "CategoryId" uuid REFERENCES recipe_categories("Id") ON DELETE SET NULL, "Cuisine" character varying(120),
                "RecipeType" character varying(80), "Status" character varying(32) NOT NULL, "CurrentVersionNumber" integer NOT NULL,
                "ActiveVersionId" uuid, "CreatedByUserId" uuid NOT NULL, "UpdatedByUserId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipes_org_code ON recipes ("OrganizationId", "RecipeCode");
            CREATE TABLE IF NOT EXISTS recipe_versions (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "RecipeId" uuid NOT NULL REFERENCES recipes("Id") ON DELETE CASCADE,
                "VersionNumber" integer NOT NULL, "Name" character varying(250) NOT NULL, "Description" text, "CategoryId" uuid,
                "Cuisine" character varying(120), "RecipeType" character varying(80), "YieldQuantity" numeric(18,6) NOT NULL,
                "YieldUom" character varying(40) NOT NULL, "PortionSize" numeric(18,6), "PortionUom" character varying(40),
                "PreparationMinutes" integer, "CookingMinutes" integer, "ImageUrl" character varying(500),
                "PreparationInstructions" text, "ChefNotes" text, "Status" character varying(32) NOT NULL,
                "EffectiveFrom" timestamp with time zone, "EffectiveTo" timestamp with time zone, "CreatedByUserId" uuid NOT NULL,
                "ApprovedByUserId" uuid, "ApprovedAt" timestamp with time zone, "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_versions_recipe_number ON recipe_versions ("RecipeId", "VersionNumber");
            CREATE TABLE IF NOT EXISTS recipe_ingredients (
                "Id" uuid PRIMARY KEY, "RecipeVersionId" uuid NOT NULL REFERENCES recipe_versions("Id") ON DELETE CASCADE,
                "MaterialId" uuid REFERENCES materials("Id") ON DELETE RESTRICT, "UnmappedIngredientName" character varying(250),
                "MaterialDescription" character varying(500), "Quantity" numeric(18,6) NOT NULL, "Uom" character varying(40) NOT NULL,
                "WastagePercent" numeric(9,4) NOT NULL, "YieldPercent" numeric(9,4) NOT NULL, "UnitCost" numeric(18,4),
                "Sequence" integer NOT NULL, "PreparationNotes" text);
            CREATE TABLE IF NOT EXISTS recipe_approval_workflows (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Event" character varying(40) NOT NULL, "LevelCount" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_approval_workflows_event ON recipe_approval_workflows ("OrganizationId", "Event");
            CREATE TABLE IF NOT EXISTS recipe_approval_levels (
                "Id" uuid PRIMARY KEY, "WorkflowId" uuid NOT NULL REFERENCES recipe_approval_workflows("Id") ON DELETE CASCADE,
                "Level" integer NOT NULL, "RoleKey" character varying(80) NOT NULL, "Label" character varying(160) NOT NULL);
            CREATE TABLE IF NOT EXISTS recipe_approval_actions (
                "Id" uuid PRIMARY KEY, "RecipeId" uuid NOT NULL REFERENCES recipes("Id") ON DELETE CASCADE,
                "RecipeVersionId" uuid NOT NULL REFERENCES recipe_versions("Id") ON DELETE CASCADE, "Event" character varying(40) NOT NULL,
                "Level" integer NOT NULL, "RoleKey" character varying(80), "ApproverUserId" uuid, "Status" character varying(24) NOT NULL,
                "Comment" text, "SubmittedAt" timestamp with time zone NOT NULL, "ActionAt" timestamp with time zone);
            CREATE TABLE IF NOT EXISTS pos_sources (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "Name" character varying(160) NOT NULL, "PosSystem" character varying(80) NOT NULL,
                "IntegrationKind" character varying(20) NOT NULL, "Status" character varying(20) NOT NULL,
                "ApiIntegrationConfigurationId" uuid REFERENCES api_integration_configurations("Id") ON DELETE SET NULL,
                "DatabaseType" character varying(40), "Host" character varying(200), "Port" integer, "DatabaseName" character varying(120),
                "SchemaName" character varying(80), "TableOrView" character varying(120), "CredentialReference" character varying(200),
                "ProtectedPassword" text, "TransactionIdField" character varying(80), "BusinessDateField" character varying(80),
                "OutletField" character varying(80), "ItemCodeField" character varying(80), "QuantityField" character varying(80),
                "StatusField" character varying(80), "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE TABLE IF NOT EXISTS pos_outlet_mappings (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "PosSourceId" uuid NOT NULL REFERENCES pos_sources("Id") ON DELETE CASCADE,
                "PosOutletCode" character varying(80) NOT NULL, "PosOutletName" character varying(160),
                "PropertyCode" character varying(40) NOT NULL, "OutletCode" character varying(80) NOT NULL,
                "PlantCode" character varying(40) NOT NULL, "StorageLocationCode" character varying(40) NOT NULL,
                "CompanyCode" character varying(40), "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_pos_outlet_mappings_code ON pos_outlet_mappings ("PosSourceId", "PosOutletCode");
            CREATE TABLE IF NOT EXISTS pos_item_recipe_mappings (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "PosSourceId" uuid NOT NULL REFERENCES pos_sources("Id") ON DELETE CASCADE,
                "PosItemCode" character varying(80) NOT NULL, "PosItemDescription" character varying(250),
                "RecipeId" uuid NOT NULL REFERENCES recipes("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_pos_item_recipe_mappings_code ON pos_item_recipe_mappings ("PosSourceId", "PosItemCode");
            CREATE TABLE IF NOT EXISTS recipe_consumption_transactions (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "PosSourceId" uuid NOT NULL,
                "SourceSystem" character varying(80) NOT NULL, "SourceTransactionId" character varying(120) NOT NULL,
                "SourceLineNumber" integer NOT NULL, "BusinessDate" date, "TransactionAt" timestamp with time zone,
                "PosOutletCode" character varying(80) NOT NULL, "PosItemCode" character varying(80) NOT NULL,
                "PosItemDescription" character varying(250), "QuantitySold" numeric(18,6) NOT NULL, "Amount" numeric(18,4),
                "RawReference" text, "Status" character varying(32) NOT NULL, "RecipeId" uuid, "RecipeVersionId" uuid,
                "RecipeVersionNumber" integer, "PropertyCode" character varying(40), "OutletCode" character varying(80),
                "CompanyCode" character varying(40), "PlantCode" character varying(40), "StorageLocationCode" character varying(40),
                "IntegrationRouteId" uuid, "IntegrationSystem" character varying(40), "ExternalReference" character varying(160),
                "FailureCode" character varying(80), "FailureMessage" text, "FailedStep" character varying(80),
                "FailureAt" timestamp with time zone, "CreatedAt" timestamp with time zone NOT NULL, "ProcessedAt" timestamp with time zone);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_consumption_identity ON recipe_consumption_transactions
                ("OrganizationId", "PosSourceId", "SourceTransactionId", "SourceLineNumber");
            CREATE TABLE IF NOT EXISTS recipe_consumption_lines (
                "Id" uuid PRIMARY KEY, "TransactionId" uuid NOT NULL REFERENCES recipe_consumption_transactions("Id") ON DELETE CASCADE,
                "MaterialId" uuid, "MaterialCode" character varying(100), "Description" character varying(500),
                "ConsumedQuantity" numeric(18,6) NOT NULL, "Uom" character varying(40) NOT NULL, "UnitCost" numeric(18,4), "Sequence" integer NOT NULL);
            CREATE TABLE IF NOT EXISTS recipe_transaction_events (
                "Id" uuid PRIMARY KEY, "TransactionId" uuid NOT NULL REFERENCES recipe_consumption_transactions("Id") ON DELETE CASCADE,
                "Step" character varying(80) NOT NULL, "Status" character varying(40) NOT NULL, "Detail" text,
                "CreatedAt" timestamp with time zone NOT NULL);
            CREATE TABLE IF NOT EXISTS recipe_inventory_postings (
                "Id" uuid PRIMARY KEY, "TransactionId" uuid NOT NULL REFERENCES recipe_consumption_transactions("Id") ON DELETE CASCADE,
                "OrganizationId" uuid NOT NULL, "Status" character varying(32) NOT NULL, "IntegrationRouteId" uuid,
                "IntegrationConfigurationId" uuid, "ExternalReference" character varying(160), "RequestJson" text, "ResponseJson" text,
                "CreatedAt" timestamp with time zone NOT NULL, "CompletedAt" timestamp with time zone);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_recipe_inventory_postings_txn ON recipe_inventory_postings ("TransactionId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS recipe_inventory_postings;
            DROP TABLE IF EXISTS recipe_transaction_events;
            DROP TABLE IF EXISTS recipe_consumption_lines;
            DROP TABLE IF EXISTS recipe_consumption_transactions;
            DROP TABLE IF EXISTS pos_item_recipe_mappings;
            DROP TABLE IF EXISTS pos_outlet_mappings;
            DROP TABLE IF EXISTS pos_sources;
            DROP TABLE IF EXISTS recipe_approval_actions;
            DROP TABLE IF EXISTS recipe_approval_levels;
            DROP TABLE IF EXISTS recipe_approval_workflows;
            DROP TABLE IF EXISTS recipe_ingredients;
            DROP TABLE IF EXISTS recipe_versions;
            DROP TABLE IF EXISTS recipes;
            DROP TABLE IF EXISTS recipe_categories;
            """);
    }
}
