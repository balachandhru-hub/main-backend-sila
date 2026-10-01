using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "api_integration_configuration",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    entity_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    process_type = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    protocol = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    resource_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    authentication_type = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    protected_password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    protected_client_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    protected_client_secret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    protected_bearer_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    token_endpoint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    token_scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    token_headers_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    token_body_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    page_size = table.Column<int>(type: "int", nullable: true),
                    watermark_field = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    last_watermark = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_successful_run_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_running = table.Column<bool>(type: "bit", nullable: false),
                    running_since = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error_safe = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    schedule_cron = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    tested_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_integration_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_event",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    old_state_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_state_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    changed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    document_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    original_filename = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    storage_reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    scan_session_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ocr_request_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    content_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    page_count = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    source_channel = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_storage_connection",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    connection_status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    tenant_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    site_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    drive_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    folder_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    folder_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    display_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    display_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    drive_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    credential_reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    validated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    connected_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_tested_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_test_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    validated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_storage_connection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "extraction_agent_config",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    endpoint_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    authentication_type = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    credential_reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    credential_last4 = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    configuration_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    enabled = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extraction_agent_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    grn_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    receipt_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    posting_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    posted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failure_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    business_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    erp_posting_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    erp_material_document = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    erp_document_year = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    erp_response_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    erp_posted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    erp_attempt_count = table.Column<int>(type: "int", nullable: false),
                    last_erp_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goods_receipt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_record",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    request_hash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    response_reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_record", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transaction",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    reference_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_transaction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_ocr_configuration",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mobile_basic_ocr_enabled = table.Column<bool>(type: "bit", nullable: false),
                    automatic_backend_fallback_enabled = table.Column<bool>(type: "bit", nullable: false),
                    minimum_mobile_confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    require_supplier_name = table.Column<bool>(type: "bit", nullable: false),
                    require_invoice_number = table.Column<bool>(type: "bit", nullable: false),
                    require_purchase_order_number = table.Column<bool>(type: "bit", nullable: false),
                    require_invoice_amount = table.Column<bool>(type: "bit", nullable: false),
                    require_invoice_date = table.Column<bool>(type: "bit", nullable: false),
                    require_currency = table.Column<bool>(type: "bit", nullable: false),
                    require_supplier_trn = table.Column<bool>(type: "bit", nullable: false),
                    backend_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    always_backend_on_reread = table.Column<bool>(type: "bit", nullable: false),
                    detailed_line_extraction_enabled = table.Column<bool>(type: "bit", nullable: false),
                    supplier_master_validation_enabled = table.Column<bool>(type: "bit", nullable: false),
                    purchase_order_validation_enabled = table.Column<bool>(type: "bit", nullable: false),
                    financial_reconciliation_enabled = table.Column<bool>(type: "bit", nullable: false),
                    amount_tolerance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    backend_timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    backend_retry_count = table.Column<int>(type: "int", nullable: false),
                    reuse_cached_ocr = table.Column<bool>(type: "bit", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_ocr_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    normalized_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    base_uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "nvarchar(16)", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    external_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "microsoft_authorization_state",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    state_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    return_url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    draft_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_microsoft_authorization_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_unit",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    parent_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(16)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_unit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    po_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    purchase_order_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    erp_supplier_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    purchasing_organization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    purchasing_group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    payment_terms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    po_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    po_date = table.Column<DateOnly>(type: "date", nullable: true),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    total_net_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    total_tax_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    total_ordered_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    total_received_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    entity_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    source_configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_last_changed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_balance",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_master",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    normalized_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    search_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    business_partner_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tax_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    trn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entity_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    legal_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    street = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    purchasing_organization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    payment_terms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_blocked = table.Column<bool>(type: "bit", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(16)", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    source_configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_last_changed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    external_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_master", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_document_storage_assignment",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_storage_destination_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    provider = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    storage_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    site_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    drive_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    folder_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    destination_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_transfer_enabled = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    validated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_document_storage_assignment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_field_mapping",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_field = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    target_field = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    transformation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    null_policy = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    default_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_validated = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_field_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_field_mapping_api_integration_configuration_configuration_id",
                        column: x => x.configuration_id,
                        principalSchema: "operations",
                        principalTable: "api_integration_configuration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_integration_execution",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trigger = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    records_read = table.Column<int>(type: "int", nullable: false),
                    records_created = table.Column<int>(type: "int", nullable: false),
                    records_updated = table.Column<int>(type: "int", nullable: false),
                    records_failed = table.Column<int>(type: "int", nullable: false),
                    watermark_before = table.Column<DateTime>(type: "datetime2", nullable: true),
                    watermark_after = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    error_message_safe = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    detail_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_integration_execution", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_integration_execution_api_integration_configuration_configuration_id",
                        column: x => x.configuration_id,
                        principalSchema: "operations",
                        principalTable: "api_integration_configuration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "integration_schema_snapshot",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    metadata_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    schema_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discovered_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_schema_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_schema_snapshot_api_integration_configuration_configuration_id",
                        column: x => x.configuration_id,
                        principalSchema: "operations",
                        principalTable: "api_integration_configuration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_extraction",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    extraction_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    raw_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    provider_reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    processing_completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    error_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extraction_method = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    fallback_used = table.Column<bool>(type: "bit", nullable: false),
                    processing_duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    trigger = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    ocr_request_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    content_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    configuration_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    structured_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_extraction", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_extraction_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "operations",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_page",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    page_number = table.Column<int>(type: "int", nullable: false),
                    storage_reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    rotation_degrees = table.Column<int>(type: "int", nullable: false),
                    ocr_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_page", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_page_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "operations",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_transfer_job",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    destination_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    document_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    resolution_source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    external_file_id = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    external_web_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_file_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    last_error_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    last_error_message_safe = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_transfer_job", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_transfer_job_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "operations",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    invoice_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_name_raw = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    supplier_tax_number_raw = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    po_number_raw = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    no_purchase_order = table.Column<bool>(type: "bit", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    net_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    tax_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    gross_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    supplier_legal_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    supplier_address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    supplier_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    supplier_phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    discount_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    freight_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    other_charges = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    taxable_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    amount_due = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    payment_terms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    extraction_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    extraction_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    extracted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    manual_edited_fields_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    invoice_type = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    overall_confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "operations",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_ext_data",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    supplier_trn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_order_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    invoice_gross = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    invoice_net = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    item_sku_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    item_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    item_net = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    item_description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    line_item_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    purchase_order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    extraction_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    extraction_confidence = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_ext_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_ext_data_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "operations",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_storage_destination",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    operating_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    storage_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    site_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    drive_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    folder_identifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    folder_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    display_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_transfer_enabled = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    validated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_storage_destination", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_storage_destination_document_storage_connection_storage_connection_id",
                        column: x => x.storage_connection_id,
                        principalSchema: "operations",
                        principalTable: "document_storage_connection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_line",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    purchase_order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    open_quantity_before = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    invoice_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    received_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    accepted_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    damaged_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    rejected_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    batch_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goods_receipt_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_receipt_line_goods_receipt_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "operations",
                        principalTable: "goods_receipt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_item",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    line_number = table.Column<int>(type: "int", nullable: false),
                    item_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ordered_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    received_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    open_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    price_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    item_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    tax_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    tax_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    gross_item_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    material_group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    plant = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    storage_location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    item_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    account_assignment_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    goods_receipt_expected = table.Column<bool>(type: "bit", nullable: false),
                    invoice_expected = table.Column<bool>(type: "bit", nullable: false),
                    delivery_completed = table.Column<bool>(type: "bit", nullable: false),
                    deletion_indicator = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    external_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    source_last_changed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_item_purchase_order_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "operations",
                        principalTable: "purchase_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_alias",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alias = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    normalized_alias = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entity_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    is_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_alias_supplier_master_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "operations",
                        principalTable: "supplier_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_material",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    supplier_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    purchase_uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_material", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_material_supplier_master_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "operations",
                        principalTable: "supplier_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    line_number = table.Column<int>(type: "int", nullable: false),
                    supplier_material_code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code_raw = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    description_raw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    tax_rate = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    tax_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    line_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    discount_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    gross_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    po_item_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    batch_number = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    match_status = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    purchase_order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_line_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "operations",
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_field_mapping_configuration_id_source_field_target_field",
                schema: "operations",
                table: "api_field_mapping",
                columns: new[] { "configuration_id", "source_field", "target_field" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_field_mapping_is_active",
                schema: "operations",
                table: "api_field_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_api_integration_configuration_is_active",
                schema: "operations",
                table: "api_integration_configuration",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_api_integration_configuration_organization_id_entity_code_process_type",
                schema: "operations",
                table: "api_integration_configuration",
                columns: new[] { "organization_id", "entity_code", "process_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_integration_configuration_status_next_run_at",
                schema: "operations",
                table: "api_integration_configuration",
                columns: new[] { "status", "next_run_at" });

            migrationBuilder.CreateIndex(
                name: "ix_api_integration_execution_configuration_id_started_at",
                schema: "operations",
                table: "api_integration_execution",
                columns: new[] { "configuration_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_api_integration_execution_is_active",
                schema: "operations",
                table: "api_integration_execution",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_is_active",
                schema: "operations",
                table: "audit_event",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_organization_id_date_created",
                schema: "operations",
                table: "audit_event",
                columns: new[] { "organization_id", "date_created" });

            migrationBuilder.CreateIndex(
                name: "ix_document_is_active",
                schema: "operations",
                table: "document",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_organization_id_content_hash",
                schema: "operations",
                table: "document",
                columns: new[] { "organization_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_document_organization_id_scan_session_id",
                schema: "operations",
                table: "document",
                columns: new[] { "organization_id", "scan_session_id" },
                unique: true,
                filter: "[scan_session_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_document_organization_id_status",
                schema: "operations",
                table: "document",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_document_extraction_document_id_extraction_type",
                schema: "operations",
                table: "document_extraction",
                columns: new[] { "document_id", "extraction_type" });

            migrationBuilder.CreateIndex(
                name: "ix_document_extraction_is_active",
                schema: "operations",
                table: "document_extraction",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_page_document_id_page_number",
                schema: "operations",
                table: "document_page",
                columns: new[] { "document_id", "page_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_page_is_active",
                schema: "operations",
                table: "document_page",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_storage_connection_is_active",
                schema: "operations",
                table: "document_storage_connection",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_storage_connection_organization_id_provider_name",
                schema: "operations",
                table: "document_storage_connection",
                columns: new[] { "organization_id", "provider", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_storage_destination_is_active",
                schema: "operations",
                table: "document_storage_destination",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_storage_destination_organization_id_storage_connection_id_document_type_operating_unit_id",
                schema: "operations",
                table: "document_storage_destination",
                columns: new[] { "organization_id", "storage_connection_id", "document_type", "operating_unit_id" },
                unique: true,
                filter: "[operating_unit_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_document_storage_destination_storage_connection_id",
                schema: "operations",
                table: "document_storage_destination",
                column: "storage_connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_transfer_job_document_id_destination_id",
                schema: "operations",
                table: "document_transfer_job",
                columns: new[] { "document_id", "destination_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_transfer_job_is_active",
                schema: "operations",
                table: "document_transfer_job",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_document_transfer_job_status_next_attempt_at",
                schema: "operations",
                table: "document_transfer_job",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_extraction_agent_config_is_active",
                schema: "operations",
                table: "extraction_agent_config",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_extraction_agent_config_organization_id_document_type",
                schema: "operations",
                table: "extraction_agent_config",
                columns: new[] { "organization_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_is_active",
                schema: "operations",
                table: "goods_receipt",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_organization_id_grn_number",
                schema: "operations",
                table: "goods_receipt",
                columns: new[] { "organization_id", "grn_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_organization_id_status",
                schema: "operations",
                table: "goods_receipt",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_line_goods_receipt_id",
                schema: "operations",
                table: "goods_receipt_line",
                column: "goods_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_line_is_active",
                schema: "operations",
                table: "goods_receipt_line",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_record_is_active",
                schema: "operations",
                table: "idempotency_record",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_record_organization_id_user_id_idempotency_key_operation",
                schema: "operations",
                table: "idempotency_record",
                columns: new[] { "organization_id", "user_id", "idempotency_key", "operation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_schema_snapshot_configuration_id",
                schema: "operations",
                table: "integration_schema_snapshot",
                column: "configuration_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_schema_snapshot_is_active",
                schema: "operations",
                table: "integration_schema_snapshot",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transaction_is_active",
                schema: "operations",
                table: "inventory_transaction",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transaction_reference_id",
                schema: "operations",
                table: "inventory_transaction",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_document_id",
                schema: "operations",
                table: "invoice",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_is_active",
                schema: "operations",
                table: "invoice",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_organization_id_invoice_number",
                schema: "operations",
                table: "invoice",
                columns: new[] { "organization_id", "invoice_number" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_organization_id_status",
                schema: "operations",
                table: "invoice",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_ext_data_document_id",
                schema: "operations",
                table: "invoice_ext_data",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_ext_data_is_active",
                schema: "operations",
                table: "invoice_ext_data",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_ext_data_organization_id",
                schema: "operations",
                table: "invoice_ext_data",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_line_invoice_id_line_number",
                schema: "operations",
                table: "invoice_line",
                columns: new[] { "invoice_id", "line_number" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_line_is_active",
                schema: "operations",
                table: "invoice_line",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_ocr_configuration_is_active",
                schema: "operations",
                table: "invoice_ocr_configuration",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_ocr_configuration_organization_id",
                schema: "operations",
                table: "invoice_ocr_configuration",
                column: "organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_is_active",
                schema: "operations",
                table: "material",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_material_organization_id_material_code",
                schema: "operations",
                table: "material",
                columns: new[] { "organization_id", "material_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_microsoft_authorization_state_is_active",
                schema: "operations",
                table: "microsoft_authorization_state",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_microsoft_authorization_state_state_hash",
                schema: "operations",
                table: "microsoft_authorization_state",
                column: "state_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_unit_is_active",
                schema: "operations",
                table: "organization_unit",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_organization_unit_organization_id_code",
                schema: "operations",
                table: "organization_unit",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_is_active",
                schema: "operations",
                table: "purchase_order",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_organization_id_entity_code_po_number",
                schema: "operations",
                table: "purchase_order",
                columns: new[] { "organization_id", "entity_code", "po_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_organization_id_operating_unit_id_status",
                schema: "operations",
                table: "purchase_order",
                columns: new[] { "organization_id", "operating_unit_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_organization_id_source_configuration_id_external_id",
                schema: "operations",
                table: "purchase_order",
                columns: new[] { "organization_id", "source_configuration_id", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_item_is_active",
                schema: "operations",
                table: "purchase_order_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_item_purchase_order_id_line_number",
                schema: "operations",
                table: "purchase_order_item",
                columns: new[] { "purchase_order_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_balance_is_active",
                schema: "operations",
                table: "stock_balance",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balance_organization_id_operating_unit_id_material_code_uom",
                schema: "operations",
                table: "stock_balance",
                columns: new[] { "organization_id", "operating_unit_id", "material_code", "uom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_alias_is_active",
                schema: "operations",
                table: "supplier_alias",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_alias_organization_id_entity_code_normalized_alias",
                schema: "operations",
                table: "supplier_alias",
                columns: new[] { "organization_id", "entity_code", "normalized_alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_alias_supplier_id",
                schema: "operations",
                table: "supplier_alias",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_master_is_active",
                schema: "operations",
                table: "supplier_master",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_master_organization_id_entity_code_supplier_code",
                schema: "operations",
                table: "supplier_master",
                columns: new[] { "organization_id", "entity_code", "supplier_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_master_organization_id_normalized_name",
                schema: "operations",
                table: "supplier_master",
                columns: new[] { "organization_id", "normalized_name" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_material_is_active",
                schema: "operations",
                table: "supplier_material",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_material_supplier_id",
                schema: "operations",
                table: "supplier_material",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_document_storage_assignment_is_active",
                schema: "operations",
                table: "user_document_storage_assignment",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_user_document_storage_assignment_user_id",
                schema: "operations",
                table: "user_document_storage_assignment",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_field_mapping",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "api_integration_execution",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "audit_event",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document_extraction",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document_page",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document_storage_destination",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document_transfer_job",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "extraction_agent_config",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "goods_receipt_line",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "idempotency_record",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "integration_schema_snapshot",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "inventory_transaction",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "invoice_ext_data",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "invoice_line",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "invoice_ocr_configuration",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "material",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "microsoft_authorization_state",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "organization_unit",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "purchase_order_item",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "stock_balance",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "supplier_alias",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "supplier_material",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "user_document_storage_assignment",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document_storage_connection",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "goods_receipt",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "api_integration_configuration",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "invoice",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "purchase_order",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "supplier_master",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "document",
                schema: "operations");
        }
    }
}
