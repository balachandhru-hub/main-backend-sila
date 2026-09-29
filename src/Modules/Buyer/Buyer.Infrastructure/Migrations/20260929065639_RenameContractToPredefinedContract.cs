using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameContractToPredefinedContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Rename tables (data-preserving; snake_case matches the project convention).
            migrationBuilder.RenameTable(
                name: "contract",
                schema: "buyersystem",
                newName: "predefined_contract",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "contract_attachment",
                schema: "buyersystem",
                newName: "predefined_contract_attachment",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "contract_approval_flow",
                schema: "buyersystem",
                newName: "predefined_contract_approval_flow",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "contract_approval_user_mapping",
                schema: "buyersystem",
                newName: "predefined_contract_approval_user_mapping",
                newSchema: "buyersystem");

            // ---- Rename primary key / foreign key constraints in place (renaming a
            // constraint does not affect enforcement, so this needs no drop/recreate).
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_contract]', N'pk_predefined_contract', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_contract_attachment]', N'pk_predefined_contract_attachment', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_contract_approval_flow]', N'pk_predefined_contract_approval_flow', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_contract_approval_user_mapping]', N'pk_predefined_contract_approval_user_mapping', N'OBJECT';");

            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_contract_buyer_business_profile_buyer_id]', N'fk_predefined_contract_buyer_business_profile_buyer_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_contract_rfq_rfqid]', N'fk_predefined_contract_rfq_rfqid', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_contract_approval_flow_contract_contract_id]', N'fk_predefined_contract_approval_flow_predefined_contract_contract_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_contract_attachment_contract_contract_id]', N'fk_predefined_contract_attachment_predefined_contract_contract_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_contract_approval_user_mapping_contract_approval_flow_contract_approval_flow_id]', N'fk_predefined_contract_approval_user_mapping_predefined_contract_approval_flow_contract_approval_flow_id', N'OBJECT';");

            // ---- Rename indexes to match the new table names.
            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_contract_buyer_id",
                newName: "ix_predefined_contract_buyer_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_contract_is_active",
                newName: "ix_predefined_contract_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_contract_rfqid",
                newName: "ix_predefined_contract_rfqid");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_flow",
                name: "ix_contract_approval_flow_contract_id",
                newName: "ix_predefined_contract_approval_flow_contract_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_flow",
                name: "ix_contract_approval_flow_is_active",
                newName: "ix_predefined_contract_approval_flow_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_user_mapping",
                name: "ix_contract_approval_user_mapping_contract_approval_flow_id",
                newName: "ix_predefined_contract_approval_user_mapping_contract_approval_flow_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_user_mapping",
                name: "ix_contract_approval_user_mapping_is_active",
                newName: "ix_predefined_contract_approval_user_mapping_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                name: "ix_contract_attachment_contract_id",
                newName: "ix_predefined_contract_attachment_contract_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                name: "ix_contract_attachment_is_active",
                newName: "ix_predefined_contract_attachment_is_active");

            // ---- Rename the contract-number sequence, preserving its current value
            // (dropping/recreating the sequence would reset numbering to its seed).
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[ContractSNSequence]', N'PredefinedContractSNSequence';");

            // ---- New attachment metadata columns.
            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "unspsc_id",
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "segment_id",
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segment_title",
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                schema: "buyersystem",
                table: "predefined_contract_attachment");

            migrationBuilder.DropColumn(
                name: "unspsc_id",
                schema: "buyersystem",
                table: "predefined_contract_attachment");

            migrationBuilder.DropColumn(
                name: "segment_id",
                schema: "buyersystem",
                table: "predefined_contract_attachment");

            migrationBuilder.DropColumn(
                name: "segment_title",
                schema: "buyersystem",
                table: "predefined_contract_attachment");

            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[PredefinedContractSNSequence]', N'ContractSNSequence';");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_predefined_contract_buyer_id",
                newName: "ix_contract_buyer_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_predefined_contract_is_active",
                newName: "ix_contract_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract",
                name: "ix_predefined_contract_rfqid",
                newName: "ix_contract_rfqid");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_flow",
                name: "ix_predefined_contract_approval_flow_contract_id",
                newName: "ix_contract_approval_flow_contract_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_flow",
                name: "ix_predefined_contract_approval_flow_is_active",
                newName: "ix_contract_approval_flow_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_user_mapping",
                name: "ix_predefined_contract_approval_user_mapping_contract_approval_flow_id",
                newName: "ix_contract_approval_user_mapping_contract_approval_flow_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_approval_user_mapping",
                name: "ix_predefined_contract_approval_user_mapping_is_active",
                newName: "ix_contract_approval_user_mapping_is_active");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                name: "ix_predefined_contract_attachment_contract_id",
                newName: "ix_contract_attachment_contract_id");

            migrationBuilder.RenameIndex(
                schema: "buyersystem",
                table: "predefined_contract_attachment",
                name: "ix_predefined_contract_attachment_is_active",
                newName: "ix_contract_attachment_is_active");

            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_predefined_contract_approval_user_mapping_predefined_contract_approval_flow_contract_approval_flow_id]', N'fk_contract_approval_user_mapping_contract_approval_flow_contract_approval_flow_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_predefined_contract_attachment_predefined_contract_contract_id]', N'fk_contract_attachment_contract_contract_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_predefined_contract_approval_flow_predefined_contract_contract_id]', N'fk_contract_approval_flow_contract_contract_id', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_predefined_contract_rfq_rfqid]', N'fk_contract_rfq_rfqid', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[fk_predefined_contract_buyer_business_profile_buyer_id]', N'fk_contract_buyer_business_profile_buyer_id', N'OBJECT';");

            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_predefined_contract_approval_user_mapping]', N'pk_contract_approval_user_mapping', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_predefined_contract_approval_flow]', N'pk_contract_approval_flow', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_predefined_contract_attachment]', N'pk_contract_attachment', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[buyersystem].[pk_predefined_contract]', N'pk_contract', N'OBJECT';");

            migrationBuilder.RenameTable(
                name: "predefined_contract_approval_user_mapping",
                schema: "buyersystem",
                newName: "contract_approval_user_mapping",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "predefined_contract_approval_flow",
                schema: "buyersystem",
                newName: "contract_approval_flow",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "predefined_contract_attachment",
                schema: "buyersystem",
                newName: "contract_attachment",
                newSchema: "buyersystem");

            migrationBuilder.RenameTable(
                name: "predefined_contract",
                schema: "buyersystem",
                newName: "contract",
                newSchema: "buyersystem");
        }
    }
}
