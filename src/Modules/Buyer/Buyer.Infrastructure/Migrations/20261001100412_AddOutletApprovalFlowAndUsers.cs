using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletApprovalFlowAndUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "master_approval_flow_id",
                schema: "buyersystem",
                table: "buyer_outlet",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "buyer_outlet_user_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_outlet_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_buyer_outlet_user_mapping_buyer_outlet_outlet_id",
                        column: x => x.outlet_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_outlet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buyer_outlet_user_mapping_is_active",
                schema: "buyersystem",
                table: "buyer_outlet_user_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_outlet_user_mapping_outlet_id",
                schema: "buyersystem",
                table: "buyer_outlet_user_mapping",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_outlet_user_mapping_user_id",
                schema: "buyersystem",
                table: "buyer_outlet_user_mapping",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buyer_outlet_user_mapping",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "master_approval_flow_id",
                schema: "buyersystem",
                table: "buyer_outlet");
        }
    }
}
