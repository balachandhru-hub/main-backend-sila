using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_predefined_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "predefined_contract");

            migrationBuilder.CreateTable(
                name: "contract_template",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    segment_id = table.Column<long>(type: "bigint", nullable: false),
                    segment_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_template_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_template_buyer_id_segment_id",
                schema: "buyersystem",
                table: "contract_template",
                columns: new[] { "buyer_id", "segment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_template_is_active",
                schema: "buyersystem",
                table: "contract_template",
                column: "is_active");

            migrationBuilder.AddForeignKey(
                name: "fk_predefined_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "predefined_contract",
                column: "buyer_id",
                principalSchema: "buyersystem",
                principalTable: "buyer_business_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_predefined_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "predefined_contract");

            migrationBuilder.DropTable(
                name: "contract_template",
                schema: "buyersystem");

            migrationBuilder.AddForeignKey(
                name: "fk_predefined_contract_buyer_business_profile_buyer_id",
                schema: "buyersystem",
                table: "predefined_contract",
                column: "buyer_id",
                principalSchema: "buyersystem",
                principalTable: "buyer_business_profile",
                principalColumn: "id");
        }
    }
}
