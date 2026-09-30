using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PredefinedMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "predefined_material",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    product_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    base_unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    alternate_unit_of_measure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    valuation_class = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit_of_measure_mapping = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sub_unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    micro_unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_predefined_material", x => x.id);
                    table.ForeignKey(
                        name: "fk_predefined_material_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "master_approval_flow",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_master_approval_flow", x => x.id);
                    table.ForeignKey(
                        name: "fk_master_approval_flow_predefined_material_material_id",
                        column: x => x.material_id,
                        principalSchema: "buyersystem",
                        principalTable: "predefined_material",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_flow_predefined_material_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_flow_predefined_material_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_flow_predefined_material_mapping_master_approval_flow_approval_flow_id",
                        column: x => x.approval_flow_id,
                        principalSchema: "buyersystem",
                        principalTable: "master_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_flow_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_flow_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_flow_user_mapping_master_approval_flow_approval_id",
                        column: x => x.approval_id,
                        principalSchema: "buyersystem",
                        principalTable: "master_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "predefined_material_approval_flow_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false),
                    comments = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_predefined_material_approval_flow_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_predefined_material_approval_flow_user_mapping_master_approval_flow_approval_id",
                        column: x => x.approval_id,
                        principalSchema: "buyersystem",
                        principalTable: "master_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_predefined_material_mapping_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_predefined_material_mapping_is_active",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_user_mapping_approval_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                column: "approval_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_user_mapping_is_active",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_master_approval_flow_is_active",
                schema: "buyersystem",
                table: "master_approval_flow",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_master_approval_flow_material_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_predefined_material_buyer_id",
                schema: "buyersystem",
                table: "predefined_material",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_predefined_material_is_active",
                schema: "buyersystem",
                table: "predefined_material",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_predefined_material_approval_flow_user_mapping_approval_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                column: "approval_id");

            migrationBuilder.CreateIndex(
                name: "ix_predefined_material_approval_flow_user_mapping_is_active",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_flow_predefined_material_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "approval_flow_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "predefined_material_approval_flow_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "master_approval_flow",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "predefined_material",
                schema: "buyersystem");
        }
    }
}
