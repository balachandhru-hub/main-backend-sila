using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilaMe.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase3InvoiceExtractionEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorCategory",
                table: "document_extractions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtractionMethod",
                table: "document_extractions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FallbackUsed",
                table: "document_extractions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ProcessingDurationMs",
                table: "document_extractions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "extraction_agent_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EndpointUrl = table.Column<string>(type: "text", nullable: true),
                    AuthenticationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CredentialLast4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extraction_agent_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_extraction_agent_configs_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_extraction_agent_configs_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_ext_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupplierTrn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    InvoiceGross = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    InvoiceNet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ItemSkuId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ItemAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ItemNet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ItemDescription = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    LineItemNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExtractionMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtractionConfidence = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_ext_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_ext_data_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_ext_data_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_ext_data_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_ext_data_purchase_order_items_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "purchase_order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_extraction_agent_configs_CreatedByUserId",
                table: "extraction_agent_configs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_extraction_agent_configs_OrganizationId_DocumentType_IsActi~",
                table: "extraction_agent_configs",
                columns: new[] { "OrganizationId", "DocumentType", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_DocumentId",
                table: "invoice_ext_data",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_ItemSkuId",
                table: "invoice_ext_data",
                column: "ItemSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_OperatingUnitId",
                table: "invoice_ext_data",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_OrganizationId",
                table: "invoice_ext_data",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_PurchaseOrderItemId",
                table: "invoice_ext_data",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_PurchaseOrderNumber",
                table: "invoice_ext_data",
                column: "PurchaseOrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_SupplierInvoiceNumber",
                table: "invoice_ext_data",
                column: "SupplierInvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ext_data_SupplierTrn",
                table: "invoice_ext_data",
                column: "SupplierTrn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extraction_agent_configs");

            migrationBuilder.DropTable(
                name: "invoice_ext_data");

            migrationBuilder.DropColumn(
                name: "ErrorCategory",
                table: "document_extractions");

            migrationBuilder.DropColumn(
                name: "ExtractionMethod",
                table: "document_extractions");

            migrationBuilder.DropColumn(
                name: "FallbackUsed",
                table: "document_extractions");

            migrationBuilder.DropColumn(
                name: "ProcessingDurationMs",
                table: "document_extractions");
        }
    }
}
