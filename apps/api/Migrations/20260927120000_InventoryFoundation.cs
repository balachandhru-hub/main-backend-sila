using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260927120000_InventoryFoundation")]
public partial class InventoryFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS inventory_locations (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "LocationCode" character varying(80) NOT NULL,
                "LocationName" character varying(200) NOT NULL,
                "LocationType" character varying(24) NOT NULL,
                "ParentLocationId" uuid REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "PropertyLocationId" uuid REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "PropertyMasterId" uuid,
                "PlantMasterId" uuid,
                "StorageLocationMasterId" uuid,
                "CompanyCodeMasterId" uuid,
                "Description" character varying(500),
                "InventoryEnabled" boolean NOT NULL,
                "SalesEnabled" boolean NOT NULL,
                "ConsumptionEnabled" boolean NOT NULL,
                "TransferEnabled" boolean NOT NULL,
                "CompanyCode" character varying(40),
                "Plant" character varying(40),
                "StorageLocation" character varying(40),
                "CostCenter" character varying(40),
                "ProfitCenter" character varying(40),
                "Currency" character varying(8),
                "ManagerGroup" character varying(80),
                "Status" character varying(16) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" uuid,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "UpdatedBy" uuid);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_inventory_locations_org_code
                ON inventory_locations ("OrganizationId", "LocationCode");
            CREATE INDEX IF NOT EXISTS IX_inventory_locations_type
                ON inventory_locations ("OrganizationId", "LocationType");

            CREATE TABLE IF NOT EXISTS inventory_balances (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE RESTRICT,
                "InventoryLocationId" uuid NOT NULL REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "BatchId" uuid,
                "OnHandQty" numeric(18,4) NOT NULL,
                "ReservedQty" numeric(18,4) NOT NULL,
                "AvailableQty" numeric(18,4) NOT NULL,
                "InTransitQty" numeric(18,4) NOT NULL,
                "BaseUom" character varying(40) NOT NULL,
                "InventoryValue" numeric(18,4) NOT NULL,
                "Currency" character varying(8),
                "LastMovementAt" timestamp with time zone,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_inventory_balances_material_location_batch
                ON inventory_balances ("MaterialId", "InventoryLocationId", "BatchId");

            CREATE TABLE IF NOT EXISTS inventory_stock_transactions (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "TransactionId" character varying(40) NOT NULL,
                "TransactionType" character varying(40) NOT NULL,
                "MaterialId" uuid NOT NULL,
                "InventoryLocationId" uuid NOT NULL,
                "Quantity" numeric(18,4) NOT NULL,
                "Uom" character varying(40) NOT NULL,
                "BaseQuantity" numeric(18,4) NOT NULL,
                "BaseUom" character varying(40) NOT NULL,
                "Direction" character varying(8) NOT NULL,
                "UnitCost" numeric(18,4),
                "TransactionValue" numeric(18,4),
                "Currency" character varying(8),
                "BatchId" uuid,
                "ExpiryDate" timestamp with time zone,
                "ReferenceType" character varying(40),
                "ReferenceId" uuid,
                "BusinessDate" timestamp with time zone NOT NULL,
                "PostingDate" timestamp with time zone NOT NULL,
                "Source" character varying(40) NOT NULL,
                "Status" character varying(16) NOT NULL,
                "CreatedBy" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_inventory_stock_transactions_org_txn
                ON inventory_stock_transactions ("OrganizationId", "TransactionId");
            CREATE INDEX IF NOT EXISTS IX_inventory_stock_transactions_material_location
                ON inventory_stock_transactions ("MaterialId", "InventoryLocationId", "CreatedAt");

            CREATE TABLE IF NOT EXISTS internal_transfer_orders (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "ItoNumber" character varying(40) NOT NULL,
                "Mode" character varying(16) NOT NULL,
                "FromInventoryLocationId" uuid NOT NULL REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "ToInventoryLocationId" uuid NOT NULL REFERENCES inventory_locations("Id") ON DELETE RESTRICT,
                "FromPropertyId" uuid,
                "ToPropertyId" uuid,
                "Reason" character varying(500),
                "RequiredBy" timestamp with time zone,
                "BusinessDate" timestamp with time zone NOT NULL,
                "Status" character varying(24) NOT NULL,
                "RequestedBy" uuid NOT NULL,
                "RequestedAt" timestamp with time zone NOT NULL,
                "ApprovedAt" timestamp with time zone,
                "DispatchedBy" uuid,
                "DispatchedAt" timestamp with time zone,
                "ReceivedBy" uuid,
                "ReceivedAt" timestamp with time zone,
                "CompletedAt" timestamp with time zone,
                "TotalValue" numeric(18,4) NOT NULL,
                "Currency" character varying(8),
                "AlreadyCollected" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_internal_transfer_orders_org_number
                ON internal_transfer_orders ("OrganizationId", "ItoNumber");

            CREATE TABLE IF NOT EXISTS internal_transfer_lines (
                "Id" uuid PRIMARY KEY,
                "ItoId" uuid NOT NULL REFERENCES internal_transfer_orders("Id") ON DELETE CASCADE,
                "MaterialId" uuid NOT NULL REFERENCES materials("Id") ON DELETE RESTRICT,
                "RequestedQty" numeric(18,4) NOT NULL,
                "ApprovedQty" numeric(18,4) NOT NULL,
                "DispatchedQty" numeric(18,4) NOT NULL,
                "ReceivedQty" numeric(18,4) NOT NULL,
                "Uom" character varying(40) NOT NULL,
                "BaseQty" numeric(18,4) NOT NULL,
                "BaseUom" character varying(40) NOT NULL,
                "UnitCost" numeric(18,4),
                "TransferValue" numeric(18,4) NOT NULL,
                "BatchId" uuid,
                "ExpiryDate" timestamp with time zone,
                "Comment" character varying(500));

            CREATE TABLE IF NOT EXISTS internal_transfer_approvals (
                "Id" uuid PRIMARY KEY,
                "ItoId" uuid NOT NULL REFERENCES internal_transfer_orders("Id") ON DELETE CASCADE,
                "Side" character varying(16) NOT NULL,
                "ManagerGroup" character varying(80),
                "Status" character varying(16) NOT NULL,
                "AvailableQty" numeric(18,4),
                "RequestedQty" numeric(18,4),
                "ApprovedQty" numeric(18,4),
                "StockAfter" numeric(18,4),
                "Comment" character varying(500),
                "ActorUserId" uuid,
                "ActedAt" timestamp with time zone);

            CREATE TABLE IF NOT EXISTS inventory_workflow_events (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "ReferenceType" character varying(40) NOT NULL,
                "ReferenceId" uuid NOT NULL,
                "Action" character varying(80) NOT NULL,
                "Comment" character varying(1000),
                "ActorUserId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_inventory_workflow_events_ref
                ON inventory_workflow_events ("ReferenceType", "ReferenceId");

            CREATE TABLE IF NOT EXISTS inventory_alerts (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "Kind" character varying(40) NOT NULL,
                "Severity" character varying(16) NOT NULL,
                "Status" character varying(24) NOT NULL,
                "Title" character varying(200),
                "Message" character varying(1000),
                "InventoryLocationId" uuid,
                "MaterialId" uuid,
                "ReferenceId" uuid,
                "ReferenceType" character varying(40),
                "RecommendedAction" character varying(40),
                "CreatedBy" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_inventory_alerts_org_status
                ON inventory_alerts ("OrganizationId", "Status");

            CREATE TABLE IF NOT EXISTS physical_inventory_requests (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "InventoryLocationId" uuid NOT NULL,
                "Reason" character varying(500) NOT NULL,
                "Priority" character varying(16) NOT NULL,
                "RequestedBy" uuid NOT NULL,
                "AssignedGroup" character varying(80),
                "ManagerVisibility" boolean NOT NULL,
                "SurpriseCount" boolean NOT NULL,
                "Status" character varying(24) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL);

            CREATE TABLE IF NOT EXISTS internal_purchase_requests (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "InventoryLocationId" uuid,
                "MaterialId" uuid,
                "Quantity" numeric(18,4) NOT NULL,
                "Uom" character varying(40),
                "Reason" character varying(500),
                "Status" character varying(32) NOT NULL,
                "RequestedBy" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL);

            CREATE TABLE IF NOT EXISTS quick_transfer_policies (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "Enabled" boolean NOT NULL,
                "AllowedSourceLocationTypes" character varying(120) NOT NULL,
                "AllowedDestinationLocationTypes" character varying(120) NOT NULL,
                "MaximumQuantity" numeric(18,4),
                "MaximumValue" numeric(18,4),
                "SamePropertyAllowed" boolean NOT NULL,
                "CrossPropertyAllowed" boolean NOT NULL,
                "SourceConfirmationRequired" boolean NOT NULL,
                "DestinationConfirmationRequired" boolean NOT NULL,
                "ManagerNotification" boolean NOT NULL,
                "SkipManagerApproval" boolean NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_quick_transfer_policies_org
                ON quick_transfer_policies ("OrganizationId");

            CREATE TABLE IF NOT EXISTS user_inventory_locations (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "InventoryLocationId" uuid NOT NULL REFERENCES inventory_locations("Id") ON DELETE CASCADE,
                "IsDefault" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_user_inventory_locations_user_location
                ON user_inventory_locations ("UserId", "InventoryLocationId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS user_inventory_locations;
            DROP TABLE IF EXISTS quick_transfer_policies;
            DROP TABLE IF EXISTS internal_purchase_requests;
            DROP TABLE IF EXISTS physical_inventory_requests;
            DROP TABLE IF EXISTS inventory_alerts;
            DROP TABLE IF EXISTS inventory_workflow_events;
            DROP TABLE IF EXISTS internal_transfer_approvals;
            DROP TABLE IF EXISTS internal_transfer_lines;
            DROP TABLE IF EXISTS internal_transfer_orders;
            DROP TABLE IF EXISTS inventory_stock_transactions;
            DROP TABLE IF EXISTS inventory_balances;
            DROP TABLE IF EXISTS inventory_locations;
            """);
    }
}
