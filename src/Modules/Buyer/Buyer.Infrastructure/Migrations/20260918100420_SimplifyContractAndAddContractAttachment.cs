using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyContractAndAddContractAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_rfqaward_rfqaward_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropTable(
                name: "contract_asset",
                schema: "buyersystem");

            migrationBuilder.DropIndex(
                name: "ix_contract_rfqaward_id_supplier_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "buyer_accepted_supplier_terms",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "buyer_esign_asset_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "buyer_terms_asset_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "contract_asset_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "is_buyer_signed",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "is_supplier_signed",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "quotation_version",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "rfqaward_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_accepted_buyer_terms",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_esign_asset_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_quotation_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "supplier_terms_asset_id",
                schema: "buyersystem",
                table: "contract");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "buyersystem",
                table: "contract",
                newName: "contract_name");

            migrationBuilder.AlterColumn<string>(
                name: "contract_number",
                schema: "buyersystem",
                table: "contract",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "contract_attachment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_attachment", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_attachment_contract_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "buyersystem",
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_attachment_contract_id",
                schema: "buyersystem",
                table: "contract_attachment",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_attachment_is_active",
                schema: "buyersystem",
                table: "contract_attachment",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_attachment",
                schema: "buyersystem");

            migrationBuilder.RenameColumn(
                name: "contract_name",
                schema: "buyersystem",
                table: "contract",
                newName: "status");

            migrationBuilder.AlterColumn<string>(
                name: "contract_number",
                schema: "buyersystem",
                table: "contract",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "buyer_accepted_supplier_terms",
                schema: "buyersystem",
                table: "contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_esign_asset_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_terms_asset_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contract_asset_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_buyer_signed",
                schema: "buyersystem",
                table: "contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_supplier_signed",
                schema: "buyersystem",
                table: "contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "quotation_version",
                schema: "buyersystem",
                table: "contract",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rfqaward_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "supplier_accepted_buyer_terms",
                schema: "buyersystem",
                table: "contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_esign_asset_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_quotation_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_terms_asset_id",
                schema: "buyersystem",
                table: "contract",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract_asset",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    uploaded_by_party = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_asset", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_asset_contract_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "buyersystem",
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_rfqaward_id_supplier_id",
                schema: "buyersystem",
                table: "contract",
                columns: new[] { "rfqaward_id", "supplier_id" },
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_contract_asset_contract_id",
                schema: "buyersystem",
                table: "contract_asset",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_asset_is_active",
                schema: "buyersystem",
                table: "contract_asset",
                column: "is_active");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_rfqaward_rfqaward_id",
                schema: "buyersystem",
                table: "contract",
                column: "rfqaward_id",
                principalSchema: "buyersystem",
                principalTable: "rfqaward",
                principalColumn: "id");
        }
    }
}
