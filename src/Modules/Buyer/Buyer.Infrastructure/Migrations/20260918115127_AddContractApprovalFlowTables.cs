using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractApprovalFlowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract_approval_flow",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approval_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_approval_flow", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_approval_flow_contract_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "buyersystem",
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_approval_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_approval_flow_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("pk_contract_approval_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_approval_user_mapping_contract_approval_flow_contract_approval_flow_id",
                        column: x => x.contract_approval_flow_id,
                        principalSchema: "buyersystem",
                        principalTable: "contract_approval_flow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_approval_flow_contract_id",
                schema: "buyersystem",
                table: "contract_approval_flow",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_approval_flow_is_active",
                schema: "buyersystem",
                table: "contract_approval_flow",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_contract_approval_user_mapping_contract_approval_flow_id",
                schema: "buyersystem",
                table: "contract_approval_user_mapping",
                column: "contract_approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_approval_user_mapping_is_active",
                schema: "buyersystem",
                table: "contract_approval_user_mapping",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_approval_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "contract_approval_flow",
                schema: "buyersystem");
        }
    }
}
