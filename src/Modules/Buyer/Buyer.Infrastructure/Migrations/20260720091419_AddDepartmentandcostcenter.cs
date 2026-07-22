using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentandcostcenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "buyer_organization_id",
                schema: "buyersystem",
                table: "rfq");

            migrationBuilder.CreateTable(
                name: "buyer_department",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_department", x => x.id);
                    table.ForeignKey(
                        name: "fk_buyer_department_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buyer_cost_center",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cost_center = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_cost_center", x => x.id);
                    table.ForeignKey(
                        name: "fk_buyer_cost_center_buyer_department_department_id",
                        column: x => x.department_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buyer_cost_center_department_id",
                schema: "buyersystem",
                table: "buyer_cost_center",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_cost_center_is_active",
                schema: "buyersystem",
                table: "buyer_cost_center",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_department_buyer_id",
                schema: "buyersystem",
                table: "buyer_department",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_department_is_active",
                schema: "buyersystem",
                table: "buyer_department",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buyer_cost_center",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_department",
                schema: "buyersystem");

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_organization_id",
                schema: "buyersystem",
                table: "rfq",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
