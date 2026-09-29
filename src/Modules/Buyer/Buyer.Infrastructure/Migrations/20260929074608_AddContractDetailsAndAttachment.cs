using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractDetailsAndAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contract_template_buyer_id_segment_id",
                schema: "buyersystem",
                table: "contract_template");

            migrationBuilder.CreateTable(
                name: "contract_details",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    predefined_contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contract_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contract_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_details_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_details_predefined_contract_predefined_contract_id",
                        column: x => x.predefined_contract_id,
                        principalSchema: "buyersystem",
                        principalTable: "predefined_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_attachment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_details_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    segment_id = table.Column<long>(type: "bigint", nullable: false),
                    segment_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        name: "fk_contract_attachment_contract_details_contract_details_id",
                        column: x => x.contract_details_id,
                        principalSchema: "buyersystem",
                        principalTable: "contract_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_template_buyer_id",
                schema: "buyersystem",
                table: "contract_template",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_attachment_contract_details_id",
                schema: "buyersystem",
                table: "contract_attachment",
                column: "contract_details_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_attachment_is_active",
                schema: "buyersystem",
                table: "contract_attachment",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_contract_details_buyer_id",
                schema: "buyersystem",
                table: "contract_details",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_details_is_active",
                schema: "buyersystem",
                table: "contract_details",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_contract_details_predefined_contract_id",
                schema: "buyersystem",
                table: "contract_details",
                column: "predefined_contract_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_attachment",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "contract_details",
                schema: "buyersystem");

            migrationBuilder.DropIndex(
                name: "ix_contract_template_buyer_id",
                schema: "buyersystem",
                table: "contract_template");

            migrationBuilder.CreateIndex(
                name: "ix_contract_template_buyer_id_segment_id",
                schema: "buyersystem",
                table: "contract_template",
                columns: new[] { "buyer_id", "segment_id" });
        }
    }
}
