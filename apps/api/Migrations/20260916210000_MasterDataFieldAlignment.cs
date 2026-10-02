using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916210000_MasterDataFieldAlignment")]
public partial class MasterDataFieldAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "suppliers"
                ADD COLUMN IF NOT EXISTS "SearchName" character varying(250),
                ADD COLUMN IF NOT EXISTS "BusinessPartnerId" character varying(100),
                ADD COLUMN IF NOT EXISTS "Trn" character varying(100),
                ADD COLUMN IF NOT EXISTS "City" character varying(150),
                ADD COLUMN IF NOT EXISTS "PostalCode" character varying(50),
                ADD COLUMN IF NOT EXISTS "Street" character varying(250),
                ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS "SourceConfigurationId" uuid,
                ADD COLUMN IF NOT EXISTS "SourceLastChangedAt" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "LastSyncedAt" timestamp with time zone;

            ALTER TABLE "supplier_aliases"
                ADD COLUMN IF NOT EXISTS "EntityCode" character varying(100) NOT NULL DEFAULT 'DEFAULT',
                ADD COLUMN IF NOT EXISTS "Confidence" numeric(5,4),
                ADD COLUMN IF NOT EXISTS "IsConfirmed" boolean NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid;

            ALTER TABLE "purchase_orders"
                ADD COLUMN IF NOT EXISTS "PurchaseOrderType" character varying(50),
                ADD COLUMN IF NOT EXISTS "CompanyCode" character varying(50),
                ADD COLUMN IF NOT EXISTS "ErpSupplierId" character varying(100),
                ADD COLUMN IF NOT EXISTS "SupplierName" character varying(250),
                ADD COLUMN IF NOT EXISTS "PurchasingOrganization" character varying(100),
                ADD COLUMN IF NOT EXISTS "PurchasingGroup" character varying(100),
                ADD COLUMN IF NOT EXISTS "PaymentTerms" character varying(100),
                ADD COLUMN IF NOT EXISTS "PoCategory" character varying(50),
                ADD COLUMN IF NOT EXISTS "TotalNetAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "TotalTaxAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "TotalAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "TotalOrderedQuantity" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "TotalReceivedQuantity" numeric(18,4);

            ALTER TABLE "purchase_order_items"
                ADD COLUMN IF NOT EXISTS "ItemNumber" character varying(50),
                ADD COLUMN IF NOT EXISTS "PriceQuantity" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "ItemAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "TaxCode" character varying(30),
                ADD COLUMN IF NOT EXISTS "TaxAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "GrossItemAmount" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "Currency" character varying(10),
                ADD COLUMN IF NOT EXISTS "MaterialGroup" character varying(100),
                ADD COLUMN IF NOT EXISTS "Plant" character varying(100),
                ADD COLUMN IF NOT EXISTS "StorageLocation" character varying(100),
                ADD COLUMN IF NOT EXISTS "ItemCategory" character varying(50),
                ADD COLUMN IF NOT EXISTS "AccountAssignmentCategory" character varying(50),
                ADD COLUMN IF NOT EXISTS "GoodsReceiptExpected" boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS "InvoiceExpected" boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS "DeliveryCompleted" boolean NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS "DeletionIndicator" boolean NOT NULL DEFAULT FALSE;
            """);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_supplier_aliases_OrganizationId_NormalizedAlias";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_supplier_aliases_OrganizationId_EntityCode_NormalizedAlias"
                ON "supplier_aliases" ("OrganizationId", "EntityCode", "NormalizedAlias");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration is additive and is not rolled back automatically.");
}