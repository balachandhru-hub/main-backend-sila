using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916140000_AdvancedInvoiceOcr")]
public partial class AdvancedInvoiceOcr : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE document_extractions
            ALTER COLUMN "ExtractionMethod" TYPE character varying(50)
            USING CASE "ExtractionMethod"
                WHEN 0 THEN 'PDF_TEXT'
                WHEN 1 THEN 'OCR'
                WHEN 2 THEN 'EXTERNAL_AGENT'
                WHEN 3 THEN 'USER_CORRECTED'
                ELSE 'PDF_TEXT'
            END;
            """);

        migrationBuilder.AddColumn<string>(
            name: "Trigger",
            table: "document_extractions",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "LEGACY");
        migrationBuilder.AddColumn<string>(name: "ContentHash", table: "document_extractions", type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ConfigurationSnapshotJson", table: "document_extractions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StructuredPayloadJson", table: "document_extractions", type: "text", nullable: true);

        migrationBuilder.CreateTable(
            name: "invoice_ocr_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                MobileBasicOcrEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AutomaticBackendFallbackEnabled = table.Column<bool>(type: "boolean", nullable: false),
                MinimumMobileConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                RequireSupplierName = table.Column<bool>(type: "boolean", nullable: false),
                RequireInvoiceNumber = table.Column<bool>(type: "boolean", nullable: false),
                RequirePurchaseOrderNumber = table.Column<bool>(type: "boolean", nullable: false),
                RequireInvoiceAmount = table.Column<bool>(type: "boolean", nullable: false),
                RequireInvoiceDate = table.Column<bool>(type: "boolean", nullable: false),
                RequireCurrency = table.Column<bool>(type: "boolean", nullable: false),
                RequireSupplierTrn = table.Column<bool>(type: "boolean", nullable: false),
                BackendProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AlwaysBackendOnReread = table.Column<bool>(type: "boolean", nullable: false),
                DetailedLineExtractionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                SupplierMasterValidationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                PurchaseOrderValidationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                FinancialReconciliationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AmountTolerance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                BackendTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                BackendRetryCount = table.Column<int>(type: "integer", nullable: false),
                ReuseCachedOcr = table.Column<bool>(type: "boolean", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invoice_ocr_configurations", x => x.Id);
                table.ForeignKey("FK_invoice_ocr_configurations_organizations_OrganizationId", x => x.OrganizationId, "organizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_invoice_ocr_configurations_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_invoice_ocr_configurations_users_UpdatedByUserId", x => x.UpdatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_invoice_ocr_configurations_OrganizationId", table: "invoice_ocr_configurations", column: "OrganizationId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_invoice_ocr_configurations_CreatedByUserId", table: "invoice_ocr_configurations", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_invoice_ocr_configurations_UpdatedByUserId", table: "invoice_ocr_configurations", column: "UpdatedByUserId");

        AddInvoiceColumn(migrationBuilder, "SupplierLegalName", "character varying(500)", 500);
        AddInvoiceColumn(migrationBuilder, "SupplierAddress", "character varying(1000)", 1000);
        AddInvoiceColumn(migrationBuilder, "SupplierEmail", "character varying(320)", 320);
        AddInvoiceColumn(migrationBuilder, "SupplierPhone", "character varying(100)", 100);
        AddInvoiceDecimalColumn(migrationBuilder, "DiscountAmount");
        AddInvoiceDecimalColumn(migrationBuilder, "FreightAmount");
        AddInvoiceDecimalColumn(migrationBuilder, "OtherCharges");
        AddInvoiceDecimalColumn(migrationBuilder, "TaxableAmount");
        AddInvoiceDecimalColumn(migrationBuilder, "AmountDue");
        AddInvoiceColumn(migrationBuilder, "PaymentTerms", "character varying(500)", 500);
        migrationBuilder.AddColumn<DateOnly>(name: "DueDate", table: "invoices", type: "date", nullable: true);
        AddInvoiceColumn(migrationBuilder, "ExtractionStatus", "character varying(40)", 40);
        AddInvoiceColumn(migrationBuilder, "ExtractionProvider", "character varying(100)", 100);
        migrationBuilder.AddColumn<DateTime>(name: "ExtractedAt", table: "invoices", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ReviewedAt", table: "invoices", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ReviewedByUserId", table: "invoices", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ManualEditedFieldsJson", table: "invoices", type: "text", nullable: true);

        AddInvoiceLineDecimalColumn(migrationBuilder, "DiscountAmount");
        AddInvoiceLineDecimalColumn(migrationBuilder, "GrossAmount");
        AddInvoiceLineColumn(migrationBuilder, "PoItemNumber", 100);
        AddInvoiceLineColumn(migrationBuilder, "BatchNumber", 150);
        migrationBuilder.AddColumn<DateOnly>(name: "ExpiryDate", table: "invoice_lines", type: "date", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "invoice_ocr_configurations");
        migrationBuilder.DropColumn("Trigger", "document_extractions");
        migrationBuilder.DropColumn("ContentHash", "document_extractions");
        migrationBuilder.DropColumn("ConfigurationSnapshotJson", "document_extractions");
        migrationBuilder.DropColumn("StructuredPayloadJson", "document_extractions");
        foreach (var name in new[] { "SupplierLegalName", "SupplierAddress", "SupplierEmail", "SupplierPhone", "PaymentTerms", "ExtractionStatus", "ExtractionProvider", "ManualEditedFieldsJson" })
            migrationBuilder.DropColumn(name, "invoices");
        foreach (var name in new[] { "DiscountAmount", "FreightAmount", "OtherCharges", "TaxableAmount", "AmountDue" })
            migrationBuilder.DropColumn(name, "invoices");
        foreach (var name in new[] { "DueDate", "ExtractedAt", "ReviewedAt", "ReviewedByUserId" })
            migrationBuilder.DropColumn(name, "invoices");
        foreach (var name in new[] { "DiscountAmount", "GrossAmount", "PoItemNumber", "BatchNumber", "ExpiryDate" })
            migrationBuilder.DropColumn(name, "invoice_lines");
    }

    private static void AddInvoiceColumn(MigrationBuilder builder, string name, string type, int maxLength) =>
        builder.AddColumn<string>(name: name, table: "invoices", type: type, maxLength: maxLength, nullable: true);

    private static void AddInvoiceDecimalColumn(MigrationBuilder builder, string name) =>
        builder.AddColumn<decimal>(name: name, table: "invoices", type: "numeric(18,4)", precision: 18, scale: 4, nullable: true);

    private static void AddInvoiceLineColumn(MigrationBuilder builder, string name, int maxLength) =>
        builder.AddColumn<string>(name: name, table: "invoice_lines", type: $"character varying({maxLength})", maxLength: maxLength, nullable: true);

    private static void AddInvoiceLineDecimalColumn(MigrationBuilder builder, string name) =>
        builder.AddColumn<decimal>(name: name, table: "invoice_lines", type: "numeric(18,4)", precision: 18, scale: 4, nullable: true);
}