using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContractTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ContractSNSequence",
                schema: "buyersystem");

            migrationBuilder.CreateTable(
                name: "contract",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqaward_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_quotation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    quotation_version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_terms_asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_terms_asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    buyer_accepted_supplier_terms = table.Column<bool>(type: "bit", nullable: false),
                    supplier_accepted_buyer_terms = table.Column<bool>(type: "bit", nullable: false),
                    buyer_esign_asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_esign_asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_buyer_signed = table.Column<bool>(type: "bit", nullable: false),
                    is_supplier_signed = table.Column<bool>(type: "bit", nullable: false),
                    contract_asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_rfqaward_rfqaward_id",
                        column: x => x.rfqaward_id,
                        principalSchema: "buyersystem",
                        principalTable: "rfqaward",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contract_asset",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    content_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    uploaded_by_party = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
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
                name: "ix_contract_is_active",
                schema: "buyersystem",
                table: "contract",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_contract_rfqaward_id_supplier_id",
                schema: "buyersystem",
                table: "contract",
                columns: new[] { "rfqaward_id", "supplier_id" },
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_contract_rfqid",
                schema: "buyersystem",
                table: "contract",
                column: "rfqid");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_asset",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "contract",
                schema: "buyersystem");

            migrationBuilder.DropSequence(
                name: "ContractSNSequence",
                schema: "buyersystem");
        }
    }
}
