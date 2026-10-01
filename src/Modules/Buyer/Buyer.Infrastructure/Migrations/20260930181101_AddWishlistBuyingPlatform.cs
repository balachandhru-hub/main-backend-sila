using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistBuyingPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buyer_outlet",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outlet_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    outlet_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    external_ship_to = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address_line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_outlet", x => x.id);
                    table.ForeignKey(
                        name: "fk_buyer_outlet_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "erp_integration_configuration",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    erp_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    create_document_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("pk_erp_integration_configuration", x => x.id);
                    table.ForeignKey(
                        name: "fk_erp_integration_configuration_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_document_integration",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    integration_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    external_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    configuration_version = table.Column<int>(type: "int", nullable: false),
                    resolved_base_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolved_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolved_http_method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolved_erp_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    last_attempt_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_attempt_on = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("pk_purchase_document_integration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wishlist",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    submitted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    submitted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    final_approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    buyer_erp_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_erp_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_erp_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_erp_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delivery_instruction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    required_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_wishlist_buyer_outlet_outlet_id",
                        column: x => x.outlet_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_outlet",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wishlist_approval_flow",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_approval_flow", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_approval_flow_wishlist_wishlist_id",
                        column: x => x.wishlist_id,
                        principalSchema: "buyersystem",
                        principalTable: "wishlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_audit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_audit_wishlist_wishlist_id",
                        column: x => x.wishlist_id,
                        principalSchema: "buyersystem",
                        principalTable: "wishlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    required_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_item_wishlist_wishlist_id",
                        column: x => x.wishlist_id,
                        principalSchema: "buyersystem",
                        principalTable: "wishlist",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wishlist_approval_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    acted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_approval_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_approval_user_mapping_wishlist_approval_flow_wishlist_approval_flow_id",
                        column: x => x.wishlist_approval_flow_id,
                        principalSchema: "buyersystem",
                        principalTable: "wishlist_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buyer_outlet_buyer_id",
                schema: "buyersystem",
                table: "buyer_outlet",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_outlet_is_active",
                schema: "buyersystem",
                table: "buyer_outlet",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_erp_integration_configuration_buyer_id",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_erp_integration_configuration_is_active",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_document_integration_is_active",
                schema: "buyersystem",
                table: "purchase_document_integration",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_document_integration_wishlist_id_integration_type_supplier_organization_id",
                schema: "buyersystem",
                table: "purchase_document_integration",
                columns: new[] { "wishlist_id", "integration_type", "supplier_organization_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_buyer_id",
                schema: "buyersystem",
                table: "wishlist",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_is_active",
                schema: "buyersystem",
                table: "wishlist",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_outlet_id",
                schema: "buyersystem",
                table: "wishlist",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_approval_flow_is_active",
                schema: "buyersystem",
                table: "wishlist_approval_flow",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_approval_flow_wishlist_id",
                schema: "buyersystem",
                table: "wishlist_approval_flow",
                column: "wishlist_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_approval_user_mapping_is_active",
                schema: "buyersystem",
                table: "wishlist_approval_user_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_approval_user_mapping_wishlist_approval_flow_id",
                schema: "buyersystem",
                table: "wishlist_approval_user_mapping",
                column: "wishlist_approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_audit_is_active",
                schema: "buyersystem",
                table: "wishlist_audit",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_audit_wishlist_id",
                schema: "buyersystem",
                table: "wishlist_audit",
                column: "wishlist_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_item_is_active",
                schema: "buyersystem",
                table: "wishlist_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_item_wishlist_id",
                schema: "buyersystem",
                table: "wishlist_item",
                column: "wishlist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "erp_integration_configuration",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "purchase_document_integration",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "wishlist_approval_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "wishlist_audit",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "wishlist_item",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "wishlist_approval_flow",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "wishlist",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_outlet",
                schema: "buyersystem");
        }
    }
}
