using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changecommentinPredefinedMaterialApprovalFlowUserMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_approval_flow_predefined_material_mapping_master_approval_flow_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping");

            migrationBuilder.DropForeignKey(
                name: "fk_approval_flow_user_mapping_master_approval_flow_approval_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping");

            migrationBuilder.DropForeignKey(
                name: "fk_master_approval_flow_predefined_material_material_id",
                schema: "buyersystem",
                table: "master_approval_flow");

            migrationBuilder.DropForeignKey(
                name: "fk_predefined_material_approval_flow_user_mapping_master_approval_flow_approval_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping");

            migrationBuilder.DropIndex(
                name: "ix_approval_flow_predefined_material_mapping_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping");

            migrationBuilder.DropColumn(
                name: "comments",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping");

            migrationBuilder.RenameColumn(
                name: "approval_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                newName: "approval_flow_predefined_material_id");

            migrationBuilder.RenameIndex(
                name: "ix_predefined_material_approval_flow_user_mapping_approval_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                newName: "ix_predefined_material_approval_flow_user_mapping_approval_flow_predefined_material_id");

            migrationBuilder.RenameColumn(
                name: "material_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                newName: "buyer_id");

            migrationBuilder.RenameIndex(
                name: "ix_master_approval_flow_material_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                newName: "ix_master_approval_flow_buyer_id");

            migrationBuilder.RenameColumn(
                name: "approval_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                newName: "approval_flow_id");

            migrationBuilder.RenameIndex(
                name: "ix_approval_flow_user_mapping_approval_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                newName: "ix_approval_flow_user_mapping_approval_flow_id");

            migrationBuilder.RenameColumn(
                name: "material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                newName: "predefined_material_id");

            migrationBuilder.AddColumn<Guid>(
                name: "approval_flow_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "comment",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_predefined_material_mapping_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "predefined_material_id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_flow_predefined_material_mapping_predefined_material_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "predefined_material_id",
                principalSchema: "buyersystem",
                principalTable: "predefined_material",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_flow_user_mapping_master_approval_flow_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                column: "approval_flow_id",
                principalSchema: "buyersystem",
                principalTable: "master_approval_flow",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_master_approval_flow_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                column: "buyer_id",
                principalSchema: "buyersystem",
                principalTable: "buyer_business_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_predefined_material_approval_flow_user_mapping_approval_flow_predefined_material_mapping_approval_flow_predefined_material_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                column: "approval_flow_predefined_material_id",
                principalSchema: "buyersystem",
                principalTable: "approval_flow_predefined_material_mapping",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_approval_flow_predefined_material_mapping_predefined_material_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping");

            migrationBuilder.DropForeignKey(
                name: "fk_approval_flow_user_mapping_master_approval_flow_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping");

            migrationBuilder.DropForeignKey(
                name: "fk_master_approval_flow_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "master_approval_flow");

            migrationBuilder.DropForeignKey(
                name: "fk_predefined_material_approval_flow_user_mapping_approval_flow_predefined_material_mapping_approval_flow_predefined_material_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping");

            migrationBuilder.DropIndex(
                name: "ix_approval_flow_predefined_material_mapping_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping");

            migrationBuilder.DropColumn(
                name: "approval_flow_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping");

            migrationBuilder.DropColumn(
                name: "comment",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping");

            migrationBuilder.RenameColumn(
                name: "approval_flow_predefined_material_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                newName: "approval_id");

            migrationBuilder.RenameIndex(
                name: "ix_predefined_material_approval_flow_user_mapping_approval_flow_predefined_material_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                newName: "ix_predefined_material_approval_flow_user_mapping_approval_id");

            migrationBuilder.RenameColumn(
                name: "buyer_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                newName: "material_id");

            migrationBuilder.RenameIndex(
                name: "ix_master_approval_flow_buyer_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                newName: "ix_master_approval_flow_material_id");

            migrationBuilder.RenameColumn(
                name: "approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                newName: "approval_id");

            migrationBuilder.RenameIndex(
                name: "ix_approval_flow_user_mapping_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                newName: "ix_approval_flow_user_mapping_approval_id");

            migrationBuilder.RenameColumn(
                name: "predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                newName: "material_id");

            migrationBuilder.AddColumn<int>(
                name: "comments",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_predefined_material_mapping_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "approval_flow_id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_flow_predefined_material_mapping_master_approval_flow_approval_flow_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "approval_flow_id",
                principalSchema: "buyersystem",
                principalTable: "master_approval_flow",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_flow_user_mapping_master_approval_flow_approval_id",
                schema: "buyersystem",
                table: "approval_flow_user_mapping",
                column: "approval_id",
                principalSchema: "buyersystem",
                principalTable: "master_approval_flow",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_master_approval_flow_predefined_material_material_id",
                schema: "buyersystem",
                table: "master_approval_flow",
                column: "material_id",
                principalSchema: "buyersystem",
                principalTable: "predefined_material",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_predefined_material_approval_flow_user_mapping_master_approval_flow_approval_id",
                schema: "buyersystem",
                table: "predefined_material_approval_flow_user_mapping",
                column: "approval_id",
                principalSchema: "buyersystem",
                principalTable: "master_approval_flow",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
