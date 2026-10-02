using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyBucketAndPersonalWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.RenameColumn(
                name: "wishlist_id",
                schema: "buyersystem",
                table: "purchase_document_integration",
                newName: "weekly_bucket_id");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_document_integration_wishlist_id_integration_type_supplier_organization_id",
                schema: "buyersystem",
                table: "purchase_document_integration",
                newName: "ix_purchase_document_integration_weekly_bucket_id_integration_type_supplier_organization_id");

            migrationBuilder.AddColumn<Guid>(
                name: "property_id",
                schema: "buyersystem",
                table: "buyer_outlet",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_location",
                schema: "buyersystem",
                table: "buyer_outlet",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "buyer_property",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    property_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_property", x => x.id);
                    table.ForeignKey(
                        name: "fk_buyer_property_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_material_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_material_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_material_mapping_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_wishlist",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_wishlist", x => x.id);
                    table.ForeignKey(
                        name: "fk_personal_wishlist_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    bucket_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    week_number = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    company_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    frozen_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    frozen_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    final_approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_bucket", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_wishlist_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    personal_wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discount_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_wishlist_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_personal_wishlist_item_personal_wishlist_personal_wishlist_id",
                        column: x => x.personal_wishlist_id,
                        principalSchema: "buyersystem",
                        principalTable: "personal_wishlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket_approval_flow",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    weekly_bucket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("pk_weekly_bucket_approval_flow", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_approval_flow_weekly_bucket_weekly_bucket_id",
                        column: x => x.weekly_bucket_id,
                        principalSchema: "buyersystem",
                        principalTable: "weekly_bucket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket_audit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weekly_bucket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("pk_weekly_bucket_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_audit_weekly_bucket_weekly_bucket_id",
                        column: x => x.weekly_bucket_id,
                        principalSchema: "buyersystem",
                        principalTable: "weekly_bucket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weekly_bucket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    original_product_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discount_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    requested_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    approved_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    supplier_stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    supplier_stock_refreshed_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    stock_in_hand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    availability_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    line_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    requestor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    storage_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_bucket_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_item_weekly_bucket_weekly_bucket_id",
                        column: x => x.weekly_bucket_id,
                        principalSchema: "buyersystem",
                        principalTable: "weekly_bucket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket_recommendation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weekly_bucket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weekly_bucket_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recommendation_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discount_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    available_stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    decided_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    decided_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_bucket_recommendation", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_recommendation_weekly_bucket_weekly_bucket_id",
                        column: x => x.weekly_bucket_id,
                        principalSchema: "buyersystem",
                        principalTable: "weekly_bucket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_bucket_approval_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weekly_bucket_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("pk_weekly_bucket_approval_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_bucket_approval_user_mapping_weekly_bucket_approval_flow_weekly_bucket_approval_flow_id",
                        column: x => x.weekly_bucket_approval_flow_id,
                        principalSchema: "buyersystem",
                        principalTable: "weekly_bucket_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buyer_property_buyer_id_plant_code",
                schema: "buyersystem",
                table: "buyer_property",
                columns: new[] { "buyer_id", "plant_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_buyer_property_is_active",
                schema: "buyersystem",
                table: "buyer_property",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_material_mapping_buyer_id_catalog_id",
                schema: "buyersystem",
                table: "catalog_material_mapping",
                columns: new[] { "buyer_id", "catalog_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_material_mapping_is_active",
                schema: "buyersystem",
                table: "catalog_material_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_personal_wishlist_buyer_id",
                schema: "buyersystem",
                table: "personal_wishlist",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_wishlist_is_active",
                schema: "buyersystem",
                table: "personal_wishlist",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_personal_wishlist_owner_user_id",
                schema: "buyersystem",
                table: "personal_wishlist",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_wishlist_item_is_active",
                schema: "buyersystem",
                table: "personal_wishlist_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_personal_wishlist_item_personal_wishlist_id",
                schema: "buyersystem",
                table: "personal_wishlist_item",
                column: "personal_wishlist_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_buyer_id_property_id_year_week_number",
                schema: "buyersystem",
                table: "weekly_bucket",
                columns: new[] { "buyer_id", "property_id", "year", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_is_active",
                schema: "buyersystem",
                table: "weekly_bucket",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_approval_flow_is_active",
                schema: "buyersystem",
                table: "weekly_bucket_approval_flow",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_approval_flow_weekly_bucket_id",
                schema: "buyersystem",
                table: "weekly_bucket_approval_flow",
                column: "weekly_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_approval_user_mapping_is_active",
                schema: "buyersystem",
                table: "weekly_bucket_approval_user_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_approval_user_mapping_weekly_bucket_approval_flow_id",
                schema: "buyersystem",
                table: "weekly_bucket_approval_user_mapping",
                column: "weekly_bucket_approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_audit_is_active",
                schema: "buyersystem",
                table: "weekly_bucket_audit",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_audit_weekly_bucket_id",
                schema: "buyersystem",
                table: "weekly_bucket_audit",
                column: "weekly_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_item_is_active",
                schema: "buyersystem",
                table: "weekly_bucket_item",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_item_weekly_bucket_id",
                schema: "buyersystem",
                table: "weekly_bucket_item",
                column: "weekly_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_recommendation_is_active",
                schema: "buyersystem",
                table: "weekly_bucket_recommendation",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_recommendation_weekly_bucket_id",
                schema: "buyersystem",
                table: "weekly_bucket_recommendation",
                column: "weekly_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_bucket_recommendation_weekly_bucket_item_id",
                schema: "buyersystem",
                table: "weekly_bucket_recommendation",
                column: "weekly_bucket_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buyer_property",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "catalog_material_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "personal_wishlist_item",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket_approval_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket_audit",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket_item",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket_recommendation",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "personal_wishlist",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket_approval_flow",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "weekly_bucket",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "property_id",
                schema: "buyersystem",
                table: "buyer_outlet");

            migrationBuilder.DropColumn(
                name: "storage_location",
                schema: "buyersystem",
                table: "buyer_outlet");

            migrationBuilder.RenameColumn(
                name: "weekly_bucket_id",
                schema: "buyersystem",
                table: "purchase_document_integration",
                newName: "wishlist_id");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_document_integration_weekly_bucket_id_integration_type_supplier_organization_id",
                schema: "buyersystem",
                table: "purchase_document_integration",
                newName: "ix_purchase_document_integration_wishlist_id_integration_type_supplier_organization_id");

            migrationBuilder.CreateTable(
                name: "wishlist",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_erp_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_erp_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    delivery_instruction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    final_approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    required_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    submitted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    submitted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    supplier_erp_document_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_erp_document_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wishlist_name = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    wishlist_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    master_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    required_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    acted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
    }
}
