using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractBuyerSupplierApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment",
                schema: "buyersystem",
                table: "contract_approval_user_mapping",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "buyersystem",
                table: "contract_approval_user_mapping",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "buyersystem",
                table: "contract",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_contract_buyer_id",
                schema: "buyersystem",
                table: "contract",
                column: "buyer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "contract",
                column: "buyer_id",
                principalSchema: "buyersystem",
                principalTable: "buyer_business_profile",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropIndex(
                name: "ix_contract_buyer_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "comment",
                schema: "buyersystem",
                table: "contract_approval_user_mapping");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "buyersystem",
                table: "contract_approval_user_mapping");

            migrationBuilder.DropColumn(
                name: "buyer_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "buyersystem",
                table: "contract");
        }
    }
}
