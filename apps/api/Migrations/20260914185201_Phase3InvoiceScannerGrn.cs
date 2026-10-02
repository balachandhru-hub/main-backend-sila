using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilaMe.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase3InvoiceScannerGrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginalFilename = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StorageReference = table.Column<string>(type: "text", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceChannel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResponseReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_idempotency_records_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_idempotency_records_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BaseUom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_materials_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_balances_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TaxNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_suppliers_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_extractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawText = table.Column<string>(type: "text", nullable: true),
                    ProviderReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_extractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_extractions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    StorageReference = table.Column<string>(type: "text", nullable: true),
                    RotationDegrees = table.Column<int>(type: "integer", nullable: false),
                    OcrStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_pages_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierMaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SupplierDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PurchaseUom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_materials_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_materials_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supplier_materials_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupplierNameRaw = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupplierTaxNumberRaw = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PoNumberRaw = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    InvoiceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoices_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OpenQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PostingProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    FailureMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipts_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_organization_units_OperatingUnitId",
                        column: x => x.OperatingUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_receipts_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    SupplierMaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialCodeRaw = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DescriptionRaw = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Uom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    LineAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    MatchStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_lines_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_lines_purchase_order_items_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "purchase_order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OpenQuantityBefore = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DamagedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_goods_receipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_purchase_order_items_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "purchase_order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_OrganizationId_CreatedAt",
                table: "audit_events",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_document_extractions_DocumentId_ExtractionType",
                table: "document_extractions",
                columns: new[] { "DocumentId", "ExtractionType" });

            migrationBuilder.CreateIndex(
                name: "IX_document_pages_DocumentId_PageNumber",
                table: "document_pages",
                columns: new[] { "DocumentId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_OperatingUnitId",
                table: "documents",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_Status",
                table: "documents",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_UploadedByUserId",
                table: "documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_GoodsReceiptId",
                table: "goods_receipt_lines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_PurchaseOrderItemId",
                table: "goods_receipt_lines",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CreatedByUserId",
                table: "goods_receipts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_InvoiceId",
                table: "goods_receipts",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_OperatingUnitId",
                table: "goods_receipts",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_OrganizationId_GrnNumber",
                table: "goods_receipts",
                columns: new[] { "OrganizationId", "GrnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_OrganizationId_Status",
                table: "goods_receipts",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_PurchaseOrderId",
                table: "goods_receipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_SupplierId",
                table: "goods_receipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_OrganizationId_UserId_IdempotencyKey_Op~",
                table: "idempotency_records",
                columns: new[] { "OrganizationId", "UserId", "IdempotencyKey", "Operation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_UserId",
                table: "idempotency_records",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_CreatedByUserId",
                table: "inventory_transactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_OperatingUnitId",
                table: "inventory_transactions",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_OrganizationId_OperatingUnitId_Creat~",
                table: "inventory_transactions",
                columns: new[] { "OrganizationId", "OperatingUnitId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_ReferenceId",
                table: "inventory_transactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceId_LineNumber",
                table: "invoice_lines",
                columns: new[] { "InvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_MaterialId",
                table: "invoice_lines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_PurchaseOrderItemId",
                table: "invoice_lines",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_CreatedByUserId",
                table: "invoices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_DocumentId",
                table: "invoices",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OperatingUnitId",
                table: "invoices",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OrganizationId_InvoiceNumber",
                table: "invoices",
                columns: new[] { "OrganizationId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OrganizationId_Status",
                table: "invoices",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OrganizationId_SupplierId",
                table: "invoices",
                columns: new[] { "OrganizationId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PurchaseOrderId",
                table: "invoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_SupplierId",
                table: "invoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_materials_OrganizationId_MaterialCode",
                table: "materials",
                columns: new[] { "OrganizationId", "MaterialCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_materials_OrganizationId_NormalizedDescription",
                table: "materials",
                columns: new[] { "OrganizationId", "NormalizedDescription" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_MaterialId",
                table: "purchase_order_items",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_PurchaseOrderId_LineNumber",
                table: "purchase_order_items",
                columns: new[] { "PurchaseOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OperatingUnitId",
                table: "purchase_orders",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_OperatingUnitId_Status",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "OperatingUnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_PoNumber",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "PoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_SupplierId",
                table: "purchase_orders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_OperatingUnitId",
                table: "stock_balances",
                column: "OperatingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_OrganizationId_OperatingUnitId_MaterialCode_~",
                table: "stock_balances",
                columns: new[] { "OrganizationId", "OperatingUnitId", "MaterialCode", "Uom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_materials_MaterialId",
                table: "supplier_materials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_materials_OrganizationId",
                table: "supplier_materials",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_materials_SupplierId_SupplierMaterialCode",
                table: "supplier_materials",
                columns: new[] { "SupplierId", "SupplierMaterialCode" });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_OrganizationId_NormalizedName",
                table: "suppliers",
                columns: new[] { "OrganizationId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_OrganizationId_SupplierCode",
                table: "suppliers",
                columns: new[] { "OrganizationId", "SupplierCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_OrganizationId_TaxNumber",
                table: "suppliers",
                columns: new[] { "OrganizationId", "TaxNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "document_extractions");

            migrationBuilder.DropTable(
                name: "document_pages");

            migrationBuilder.DropTable(
                name: "goods_receipt_lines");

            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "stock_balances");

            migrationBuilder.DropTable(
                name: "supplier_materials");

            migrationBuilder.DropTable(
                name: "goods_receipts");

            migrationBuilder.DropTable(
                name: "purchase_order_items");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
