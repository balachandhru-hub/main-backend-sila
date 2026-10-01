using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierErpIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_erp_integration_configuration",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    erp_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    auth_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    http_method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    auth_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    token_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    client_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    client_secret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    api_key_header = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    api_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    access_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    default_ship_to = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order_date_format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    headers_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    max_retry_count = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_erp_integration_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_purchase_document",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    buyer_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    configuration_version = table.Column<int>(type: "int", nullable: false),
                    resolved_base_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolved_order_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    last_attempt_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    response_body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    outcome_unknown = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_purchase_document", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_erp_integration_configuration_is_active",
                schema: "supplier",
                table: "supplier_erp_integration_configuration",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_purchase_document_idempotency_key",
                schema: "supplier",
                table: "supplier_purchase_document",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_purchase_document_is_active",
                schema: "supplier",
                table: "supplier_purchase_document",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_erp_integration_configuration",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_purchase_document",
                schema: "supplier");
        }
    }
}
